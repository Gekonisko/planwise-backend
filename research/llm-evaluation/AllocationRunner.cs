using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Optimisation;
using PlanWise.Modules.Scheduling.Infrastructure.Llm;
using PlanWise.Modules.Scheduling.Infrastructure.Optimisation;

namespace PlanWise.Research.LlmEvaluation;

/// <summary>
/// Runs every allocation method over the same instances and scores them all with the same function.
///
/// The four arms differ in exactly one respect each, which is what makes the comparison attributable:
/// greedy and CP-SAT are the two non-LLM baselines already in the product; the single-shot arm is the
/// agent with its turn budget set to one, so it must commit without ever seeing a consequence; the
/// agentic arm is the same class, same prompt, same schema, with the budget raised. Any difference
/// between the last two is the loop, not the wording.
///
/// CP-SAT supplies the reference. It proves optimality for the makespan term on these instances, so
/// every other arm can be reported as a percentage above a known optimum rather than against another
/// heuristic — the closest thing to ground truth available without real project outcomes, and the
/// reason this study can talk about efficiency at all.
/// </summary>
internal static class AllocationRunner
{
    internal const string ArmGreedy = "greedy";
    internal const string ArmCpSat = "cpsat";
    internal const string ArmSingleShot = "llm-single-shot";
    internal const string ArmAgent = "llm-agent";

    internal sealed record AllocationRunRecord(
        string ScenarioId,
        string Description,
        string Arm,
        int Attempt,
        double ElapsedSeconds,
        int HttpCalls,
        Usage Usage,
        AllocationRunMetrics? Metrics,
        string? ExpectedGain,
        string? Error,
        string? ApiError);

    /// <summary>
    /// The first non-success status the transport saw, with its body. An LLM arm that falls back to
    /// the greedy balancer looks, in the result object, exactly like an LLM arm that chose the greedy
    /// answer — this is what tells the two apart, and a whole series has already been spent finding
    /// that out the hard way.
    /// </summary>
    internal static string? FirstApiError(RecordingHandler recorder) =>
        recorder.Exchanges
            .Where(exchange => exchange.StatusCode is < 200 or >= 300)
            .Select(exchange => $"HTTP {exchange.StatusCode}: {exchange.ResponseBody}")
            .FirstOrDefault();

    /// <param name="GapPercent">
    /// How far above the proven optimum this allocation finishes, in percent. Zero means the method
    /// matched the solver. Null when the allocation is incomplete, and that is not squeamishness: a
    /// task nobody owns occupies nobody's timeline, so an arm that allocates nothing scores the
    /// shortest project of all. Leaving work undone must not be able to look like efficiency, so the
    /// gap is defined only over complete allocations and Complete is reported beside it.
    /// </param>
    /// <param name="Complete">Every open task ended up with an owner. A precondition for GapPercent to mean anything.</param>
    /// <param name="FellBackToGreedy">
    /// True when the LLM arm produced nothing usable and the greedy balancer answered instead. These
    /// runs must be reported separately: counting them as agent results would quietly credit the
    /// agent with a deterministic algorithm's work.
    /// </param>
    internal sealed record AllocationRunMetrics(
        string ModelName,
        long MakespanDays,
        long ReferenceMakespanDays,
        double? GapPercent,
        bool Complete,
        int ProposedAssignments,
        int OpenTasksLeftUnassigned,
        int InvalidAssignments,
        int SkillMatchedTasks,
        int TasksPastDueDate,
        int BusiestMemberDays,
        int QuietestMemberDays,
        int LoadSpreadDays,
        bool FellBackToGreedy);

    public static IScheduleOptimisationModel Greedy() => new GreedyCapacityBalancer();

    public static IScheduleOptimisationModel CpSat() =>
        new CpSatScheduleOptimiser(NullLogger<CpSatScheduleOptimiser>.Instance);

    /// <summary>
    /// The agent, built by reflection because it is internal to the Scheduling infrastructure — the
    /// same approach the cost and prioritisation models take, and for the same reason: a research
    /// harness should not force production assemblies to widen their visibility.
    /// </summary>
    public static IScheduleOptimisationModel Agent(HttpClient httpClient, AnthropicSchedulingOptions options)
    {
        Type type = typeof(AnthropicSchedulingOptions).Assembly
            .GetType("PlanWise.Modules.Scheduling.Infrastructure.Llm.AgenticScheduleOptimiser", throwOnError: true)!;

        // NullLogger<T>.Instance is a static *field*, unlike the non-generic NullLogger.Instance
        // property — hence both lookups, with construction as the last resort.
        Type loggerType = typeof(NullLogger<>).MakeGenericType(type);
        object logger =
            loggerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? loggerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? Activator.CreateInstance(loggerType, nonPublic: true)!;

        return (IScheduleOptimisationModel)Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: [httpClient, Options.Create(options), logger],
            culture: null)!;
    }

    /// <summary>
    /// The optimum the other arms are measured against. Run once per scenario rather than once per
    /// attempt: CP-SAT is deterministic enough on these instances that re-solving would only add
    /// wall-clock, and a moving reference would make the gap figures incomparable between arms.
    /// </summary>
    public static async Task<long> ReferenceMakespanAsync(ScheduleOptimisationInput input)
    {
        ScheduleOptimisationResult result = await CpSat().OptimiseAsync(input);
        AssignmentEvaluation evaluation = Evaluate(input, result);
        return evaluation.MakespanDays;
    }

    public static async Task<AllocationRunRecord> RunAsync(
        IScheduleOptimisationModel model,
        string arm,
        RecordingHandler recorder,
        AllocationScenarios.AllocationScenarioDefinition scenario,
        int attempt,
        long referenceMakespan)
    {
        recorder.Clear();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            ScheduleOptimisationResult result = await model.OptimiseAsync(scenario.Input);
            stopwatch.Stop();

            AssignmentEvaluation evaluation = Evaluate(scenario.Input, result);
            int[] busyDays = evaluation.MemberLoads.Select(load => load.BusyDays).ToArray();

            var metrics = new AllocationRunMetrics(
                result.ModelName,
                evaluation.MakespanDays,
                referenceMakespan,
                Gap(evaluation.MakespanDays, referenceMakespan, evaluation.UnassignedTaskKeys.Count),
                evaluation.UnassignedTaskKeys.Count == 0,
                result.Assignments.Count,
                evaluation.UnassignedTaskKeys.Count,
                evaluation.Violations.Count,
                evaluation.SkillMatchedTasks,
                evaluation.TasksPastDueDate,
                busyDays.Length == 0 ? 0 : busyDays.Max(),
                busyDays.Length == 0 ? 0 : busyDays.Min(),
                busyDays.Length == 0 ? 0 : busyDays.Max() - busyDays.Min(),
                result.ModelName.Contains("fallback", StringComparison.OrdinalIgnoreCase));

            return new AllocationRunRecord(
                scenario.Id, scenario.Description, arm, attempt,
                stopwatch.Elapsed.TotalSeconds, recorder.Exchanges.Count, Usage.From(recorder.Exchanges),
                metrics, result.ExpectedGain, Error: null, ApiError: FirstApiError(recorder));
        }
        // Catches everything on purpose: how a method fails is a result, and the previous series was
        // cut short by an exception type the production retry loop did not expect.
        catch (Exception exception)
        {
            stopwatch.Stop();
            return new AllocationRunRecord(
                scenario.Id, scenario.Description, arm, attempt,
                stopwatch.Elapsed.TotalSeconds, recorder.Exchanges.Count, Usage.From(recorder.Exchanges),
                Metrics: null, ExpectedGain: null,
                Error: $"{exception.GetType().Name}: {exception.Message}",
                ApiError: FirstApiError(recorder));
        }
    }

    private static AssignmentEvaluation Evaluate(ScheduleOptimisationInput input, ScheduleOptimisationResult result) =>
        ScheduleEvaluator.Evaluate(
            input,
            result.Assignments
                .GroupBy(assignment => assignment.TaskId)
                .ToDictionary(group => group.Key, group => group.First().ProposedAssigneeId));

    private static double? Gap(long makespan, long reference, int unassigned)
    {
        if (makespan < 0 || reference <= 0 || unassigned > 0)
        {
            return null;
        }

        return (makespan - reference) / (double)reference * 100.0;
    }
}

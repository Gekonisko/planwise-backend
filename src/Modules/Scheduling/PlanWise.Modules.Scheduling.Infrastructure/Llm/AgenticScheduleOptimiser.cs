using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Optimisation;
using PlanWise.Modules.Scheduling.Infrastructure.Optimisation;

namespace PlanWise.Modules.Scheduling.Infrastructure.Llm;

// The third implementation of IScheduleOptimisationModel, and the only one that is an agent.
//
// WHAT MAKES IT AN AGENT, AND WHY THAT IS THE POINT. The other two LLM calls in this codebase
// (AnthropicCostEstimationModel, AnthropicBacklogPrioritisationModel) are single-shot: one request,
// one forced tool call, parse, done. The model never learns what its answer was worth. Here it does.
// It is given two tools — evaluate_assignment, which runs the real ScheduleEvaluator over a proposal
// and hands back the project's finish date, each member's load and any rule it broke, and
// submit_assignment, which ends the loop. So it can propose, observe the consequence in days, and
// revise. Perceive, act, observe, revise: that is the loop the single-shot arm does not have, and
// isolating its effect is the experiment.
//
// WHY THE SAME EVALUATOR SCORES IT. evaluate_assignment is not a private scratchpad — it is the
// identical function the study uses to score CP-SAT, the greedy balancer and the single-shot arm. An
// agent that games it games the measurement in the open, which is the only defensible arrangement.
//
// ITS OWN CONTROL GROUP. At MaxTurns = 1 the first turn is also the last, so tool_choice forces
// submit_assignment and the model must commit having evaluated nothing: that is exactly the
// single-shot behaviour of the other two LLM call sites, under an identical prompt and schema. The
// loop budget is therefore the only variable that changes between the arms, which is what lets the
// difference between them be attributed to the loop rather than to prompt wording.
//
// HONESTY ABOUT WHAT THIS CANNOT BEAT. CpSatScheduleOptimiser proves optimality for the stated
// objective. On the makespan term an agent can at very best tie it, never win, and each of its turns
// costs a billed request where the solver costs none. This class exists to measure how close a
// language model gets to a proven optimum and what that proximity costs — not to replace the solver.
//
// WHY IT CAN STILL FALL BACK. The agent may exhaust its turns without ever committing, or name a
// task key or member that does not exist. The honest answer then is the greedy result, labelled as
// such in the model name, exactly as CpSatScheduleOptimiser does when the solver cannot deliver.
internal sealed class AgenticScheduleOptimiser(
    HttpClient httpClient,
    IOptions<AnthropicSchedulingOptions> options,
    ILogger<AgenticScheduleOptimiser> logger) : IScheduleOptimisationModel
{
    public const string Name = "AgenticScheduleOptimiser v1";

    private const string EvaluateTool = "evaluate_assignment";
    private const string SubmitTool = "submit_assignment";

    private const string ObjectiveText =
        "Minimise the project's finish date (makespan) subject to dependency order, one task at a time per member and capacity-scaled durations, proposed by an LLM agent that may score and revise its own candidate assignments before committing";

    public async Task<ScheduleOptimisationResult> OptimiseAsync(
        ScheduleOptimisationInput input,
        CancellationToken cancellationToken = default)
    {
        var members = input.Members
            .Where(member => member.UserId is not null && member.Capacity > 0)
            .ToList();
        var unassigned = input.Tasks.Where(task => !task.IsDone && task.AssigneeId is null).ToList();

        if (unassigned.Count == 0 || members.Count == 0)
        {
            return new ScheduleOptimisationResult(
                Name, ObjectiveText, [], [], [],
                unassigned.Count == 0
                    ? "No unassigned tasks to schedule — nothing proposed"
                    : "No members with available capacity — nothing proposed");
        }

        try
        {
            ScheduleOptimisationResult? agentResult = await RunAgentAsync(input, members, unassigned, cancellationToken);
            if (agentResult is not null)
            {
                return agentResult;
            }

            logger.LogWarning(
                "The scheduling agent for project {ProjectId} used all {Turns} turns without submitting an assignment; falling back to the greedy balancer.",
                input.ProjectId, options.Value.MaxTurns);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or InvalidOperationException)
        {
            logger.LogError(exception, "The scheduling agent failed for project {ProjectId}; falling back to the greedy balancer.", input.ProjectId);
        }

        return FellBackToGreedy(input);
    }

    private async Task<ScheduleOptimisationResult?> RunAgentAsync(
        ScheduleOptimisationInput input,
        List<ProjectMemberSummary> members,
        List<ScheduleTaskSummary> unassigned,
        CancellationToken cancellationToken)
    {
        var taskByKey = input.Tasks.ToDictionary(task => task.Key, StringComparer.OrdinalIgnoreCase);
        var memberByEmail = members.ToDictionary(member => member.Email, StringComparer.OrdinalIgnoreCase);

        var messages = new JsonArray
        {
            new JsonObject
            {
                ["role"] = "user",
                ["content"] = BuildBriefing(input, members, unassigned)
            }
        };

        int maxTurns = Math.Max(1, options.Value.MaxTurns);
        int evaluations = 0;

        for (int turn = 1; turn <= maxTurns; turn++)
        {
            // On the final turn the choice is taken away: the agent must commit to something rather
            // than spend its last request on another evaluation and leave us with nothing.
            bool lastTurn = turn == maxTurns;
            JsonNode response = await SendAsync(messages, lastTurn, cancellationToken);

            JsonArray content = response["content"]?.AsArray()
                ?? throw new JsonException("Anthropic response contained no content block");

            messages.Add(new JsonObject
            {
                ["role"] = "assistant",
                ["content"] = content.DeepClone()
            });

            var toolResults = new JsonArray();

            foreach (JsonNode? block in content)
            {
                if (block?["type"]?.GetValue<string>() != "tool_use")
                {
                    continue;
                }

                string toolName = block["name"]?.GetValue<string>() ?? string.Empty;
                string toolUseId = block["id"]?.GetValue<string>() ?? string.Empty;
                JsonNode? toolInput = block["input"];

                if (toolName == SubmitTool)
                {
                    Dictionary<Guid, Guid> proposed = ReadAssignments(toolInput, taskByKey, memberByEmail);
                    string rationale = toolInput?["rationale"]?.GetValue<string>() ?? "No rationale given";
                    return BuildResult(input, unassigned, proposed, rationale, turn, evaluations);
                }

                if (toolName == EvaluateTool)
                {
                    evaluations++;
                    Dictionary<Guid, Guid> candidate = ReadAssignments(toolInput, taskByKey, memberByEmail);
                    AssignmentEvaluation evaluation = ScheduleEvaluator.Evaluate(input, candidate);

                    toolResults.Add(new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = toolUseId,
                        ["content"] = DescribeEvaluation(evaluation)
                    });
                }
            }

            if (toolResults.Count == 0)
            {
                return null;
            }

            messages.Add(new JsonObject { ["role"] = "user", ["content"] = toolResults });
        }

        return null;
    }

    private async Task<JsonNode> SendAsync(JsonArray messages, bool forceSubmit, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["model"] = options.Value.Model,
            // 16384, not 8192: on a 40-task instance a turn that restates the whole allocation ran
            // past the smaller cap, came back with stop_reason "max_tokens" and no usable tool call,
            // and the optimiser fell back to the greedy balancer. The cap was never binding on the
            // smaller instances, so raising it changes nothing there.
            ["max_tokens"] = 16384,
            ["system"] = SystemPrompt,
            ["messages"] = messages.DeepClone(),
            // On the final turn evaluate_assignment is withheld, not merely discouraged. An evaluation
            // returned on the last turn can never be acted on, so offering it is a trap: measured
            // behaviour was to spend the forced submit on an empty list and then emit the allocation
            // it actually wanted as a trailing evaluate call, which no caller can honour. Withholding
            // it also makes MaxTurns = 1 a true single-shot control — one tool, forced, exactly as
            // AnthropicCostEstimationModel is configured.
            ["tools"] = forceSubmit
                ? new JsonArray { SubmitToolDefinition() }
                : new JsonArray { EvaluateToolDefinition(), SubmitToolDefinition() },
            // "any" rather than "auto": every turn must move the loop forward, and a turn spent on
            // prose is a billed request that changes nothing.
            ["tool_choice"] = forceSubmit
                ? new JsonObject { ["type"] = "tool", ["name"] = SubmitTool }
                : new JsonObject { ["type"] = "any" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", options.Value.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Anthropic API request failed with status {(int)response.StatusCode}: {responseBody}");
        }

        return JsonNode.Parse(responseBody) ?? throw new JsonException("Anthropic response was not valid JSON");
    }

    private const string SystemPrompt =
        "You allocate unassigned backlog tasks to members of a software project team. Your goal is to " +
        "make the project finish as early as possible.\n\n" +
        "How the schedule works, exactly:\n" +
        "- A task takes one day per story point at full capacity, rounded up after dividing by the " +
        "assignee's capacity. A 3-point task given to a 0.5-capacity member takes 6 days.\n" +
        "- A member does one task at a time. Two tasks on the same person cannot overlap.\n" +
        "- A task cannot start until every unfinished predecessor has finished.\n" +
        "- Tasks that already have an assignee keep that assignee and still occupy their owner's time.\n\n" +
        "Method you must follow:\n" +
        "1. Work out which tasks lie on the longest dependency chain — those decide the finish date, " +
        "so give them to members who are free and fast.\n" +
        "2. Call evaluate_assignment with a complete candidate allocation. It returns the real finish " +
        "date in days, each member's load, and any rule you broke.\n" +
        "3. Read the result. If one member is the bottleneck, move work off them and evaluate again.\n" +
        "4. Call submit_assignment with your best allocation.\n\n" +
        "Assign every unassigned task — leaving one out does not make the project shorter, it just " +
        "leaves the work undone. Prefer a member whose skill tags match the task title when it costs " +
        "nothing in days. Use task keys and member emails exactly as given.";

    private static string BuildBriefing(
        ScheduleOptimisationInput input,
        List<ProjectMemberSummary> members,
        List<ScheduleTaskSummary> unassigned)
    {
        var keyById = input.Tasks.ToDictionary(task => task.TaskId, task => task.Key);
        var builder = new StringBuilder();

        builder.AppendLine(CultureInfo.InvariantCulture, $"Today is day 0 ({input.Today:yyyy-MM-dd}).");
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Team ({members.Count} members):");
        foreach (ProjectMemberSummary member in members)
        {
            string skills = member.Skills.Count == 0 ? "none listed" : string.Join(", ", member.Skills);
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"- {member.Email} | role: {member.Role} | capacity: {member.Capacity} | skills: {skills}");
        }

        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Tasks ({input.Tasks.Count(task => !task.IsDone)} open):");
        foreach (ScheduleTaskSummary task in input.Tasks.Where(task => !task.IsDone))
        {
            var facts = new List<string> { $"{task.Points ?? 1} point(s)" };

            IEnumerable<string> predecessors = task.PredecessorTaskIds
                .Where(keyById.ContainsKey)
                .Select(id => keyById[id]);
            string predecessorList = string.Join(", ", predecessors);
            if (predecessorList.Length > 0)
            {
                facts.Add($"after {predecessorList}");
            }

            if (task.DueDate is DateOnly due)
            {
                facts.Add($"due day {Math.Max(0, due.DayNumber - input.Today.DayNumber)}");
            }

            facts.Add(task.AssigneeId is null
                ? "UNASSIGNED — you must allocate this one"
                : "already assigned, do not touch");

            builder.AppendLine(CultureInfo.InvariantCulture, $"- {task.Key}: {task.Title} [{string.Join("; ", facts)}]");
        }

        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Allocate all {unassigned.Count} unassigned task(s). Evaluate at least one candidate before you submit.");

        return builder.ToString();
    }

    /// <summary>
    /// The feedback the agent acts on. Deliberately factual and free of advice — the point of the
    /// experiment is what the model does with an honest measurement, not how well it follows a hint.
    /// </summary>
    private static string DescribeEvaluation(AssignmentEvaluation evaluation)
    {
        var builder = new StringBuilder();

        builder.AppendLine(evaluation.Feasible
            ? string.Create(CultureInfo.InvariantCulture, $"Project finishes on day {evaluation.MakespanDays}.")
            : "This allocation could not be scheduled at all.");

        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Tasks with an owner: {evaluation.AssignedTasks}; skill-matched: {evaluation.SkillMatchedTasks}; past their due date: {evaluation.TasksPastDueDate}.");

        builder.AppendLine("Load per member (busy days / tasks):");
        foreach (MemberLoad load in evaluation.MemberLoads)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {load.Email}: {load.BusyDays} day(s) across {load.TaskCount} task(s)");
        }

        if (evaluation.UnassignedTaskKeys.Count > 0)
        {
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"STILL UNASSIGNED ({evaluation.UnassignedTaskKeys.Count}): {string.Join(", ", evaluation.UnassignedTaskKeys)}");
        }

        if (evaluation.Violations.Count > 0)
        {
            builder.AppendLine("REJECTED ASSIGNMENTS (these were ignored):");
            foreach (string violation in evaluation.Violations)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {violation}");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Task keys and member emails, never ids. A GUID is something a language model can plausibly
    /// invent; a key it was shown two paragraphs earlier is not, and an unknown one is caught here
    /// rather than becoming a silent no-op.
    /// </summary>
    private static Dictionary<Guid, Guid> ReadAssignments(
        JsonNode? toolInput,
        Dictionary<string, ScheduleTaskSummary> taskByKey,
        Dictionary<string, ProjectMemberSummary> memberByEmail)
    {
        var assignments = new Dictionary<Guid, Guid>();
        JsonArray? rows = toolInput?["assignments"]?.AsArray();

        if (rows is null)
        {
            return assignments;
        }

        foreach (JsonNode? row in rows)
        {
            string? key = row?["taskKey"]?.GetValue<string>();
            string? email = row?["assigneeEmail"]?.GetValue<string>();

            if (key is null || email is null)
            {
                continue;
            }

            if (taskByKey.TryGetValue(key, out ScheduleTaskSummary? task)
                && memberByEmail.TryGetValue(email, out ProjectMemberSummary? member))
            {
                assignments[task.TaskId] = member.UserId!.Value;
            }
        }

        return assignments;
    }

    private ScheduleOptimisationResult BuildResult(
        ScheduleOptimisationInput input,
        List<ScheduleTaskSummary> unassigned,
        Dictionary<Guid, Guid> proposed,
        string rationale,
        int turns,
        int evaluations)
    {
        var emailById = input.Members
            .Where(member => member.UserId is not null)
            .ToDictionary(member => member.UserId!.Value, member => member.Email);
        var assignments = unassigned
            .Where(task => proposed.ContainsKey(task.TaskId))
            .Select(task => new ProposedTaskAssignment(
                task.TaskId, task.Key, task.AssigneeId, proposed[task.TaskId], emailById[proposed[task.TaskId]]))
            .ToList();

        AssignmentEvaluation evaluation = ScheduleEvaluator.Evaluate(input, proposed);
        ScheduleOptimisationResult greedy = GreedyCapacityBalancer.Optimise(input);
        AssignmentEvaluation greedyEvaluation = ScheduleEvaluator.Evaluate(
            input, greedy.Assignments.ToDictionary(a => a.TaskId, a => a.ProposedAssigneeId));

        var relaxed = new List<string>
        {
            "The allocation was chosen by a language model, which offers no optimality guarantee — a constraint solver on the same input can only do better or equal on the finish date",
            "Skill matching is a title-substring heuristic, not true competency matching — no task carries a structured required-skill field",
            "Member calendar-specific availability (holidays, leave) not modelled — capacity is treated as a constant figure, and every day is a working day",
            "One story point is treated as one day of work at full capacity, since ProjectTask has no separate estimate field"
        };

        if (evaluation.UnassignedTaskKeys.Count > 0)
        {
            relaxed.Insert(0, $"The agent left {evaluation.UnassignedTaskKeys.Count} task(s) without an owner: {string.Join(", ", evaluation.UnassignedTaskKeys)}");
        }

        if (evaluation.Violations.Count > 0)
        {
            relaxed.Insert(0, $"{evaluation.Violations.Count} proposed assignment(s) were rejected as invalid and discarded");
        }

        string comparison = (evaluation.Feasible, greedyEvaluation.Feasible) switch
        {
            (false, _) => "; this allocation could not be scheduled",
            (_, false) => "; the greedy baseline could not be scheduled for comparison",
            _ when greedyEvaluation.MakespanDays > evaluation.MakespanDays =>
                $", {greedyEvaluation.MakespanDays - evaluation.MakespanDays} day(s) sooner than the greedy load-balancer's assignment of the same work",
            _ when greedyEvaluation.MakespanDays == evaluation.MakespanDays =>
                ", the same finish date the greedy load-balancer reaches on this project",
            _ => $", {evaluation.MakespanDays - greedyEvaluation.MakespanDays} day(s) later than the greedy load-balancer's assignment of the same work",
        };

        return new ScheduleOptimisationResult(
            Name,
            ObjectiveText,
            assignments,
            [
                "Dependency order is enforced: no task starts before every unfinished predecessor has finished",
                "No member is given two tasks at the same time",
                "Task duration scales with the assignee's capacity — a half-time member takes twice as long",
                "Existing assignments on already-assigned tasks were not changed, but they do occupy their owner's timeline",
                "Completed tasks were not reassigned",
                $"The agent scored {evaluations} candidate allocation(s) against the schedule before committing, over {turns} turn(s)"
            ],
            relaxed,
            $"Project finishes in {evaluation.MakespanDays} day(s) (around {input.Today.AddDays((int)Math.Clamp(evaluation.MakespanDays, 0, 3_650)):yyyy-MM-dd}){comparison}. " +
            $"{assignments.Count} assignment(s) proposed, {evaluation.SkillMatchedTasks} matching on skill tags. Agent's reasoning: {rationale}");
    }

    private static ScheduleOptimisationResult FellBackToGreedy(ScheduleOptimisationInput input)
    {
        ScheduleOptimisationResult greedy = GreedyCapacityBalancer.Optimise(input);
        return greedy with
        {
            ModelName = $"{GreedyCapacityBalancer.Name} (fallback)",
            ConstraintsRelaxed =
            [
                "The scheduling agent did not return a usable allocation, so these assignments come from the greedy load-balancer instead — they balance workload but are not optimised for the project's finish date",
                .. greedy.ConstraintsRelaxed
            ],
        };
    }

    private static JsonObject AssignmentsSchema() =>
        new()
        {
            ["type"] = "array",
            ["description"] = "One entry per task you are allocating, using the exact task keys and member emails from the briefing.",
            ["items"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["taskKey"] = new JsonObject { ["type"] = "string" },
                    ["assigneeEmail"] = new JsonObject { ["type"] = "string" }
                },
                ["required"] = new JsonArray { "taskKey", "assigneeEmail" }
            }
        };

    private static JsonObject EvaluateToolDefinition() =>
        new()
        {
            ["name"] = EvaluateTool,
            ["description"] =
                "Score a candidate allocation against the real project schedule. Returns the day the project would finish, " +
                "each member's load in busy days, how many tasks miss their due date, and any assignment that was rejected. " +
                "Call this before submitting, and again after any change you are not certain about.",
            ["input_schema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject { ["assignments"] = AssignmentsSchema() },
                ["required"] = new JsonArray { "assignments" }
            }
        };

    private static JsonObject SubmitToolDefinition() =>
        new()
        {
            ["name"] = SubmitTool,
            ["description"] = "Commit to your final allocation. This ends your turn — you cannot evaluate again afterwards.",
            ["input_schema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["assignments"] = AssignmentsSchema(),
                    ["rationale"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "One or two sentences on why this allocation finishes the project soonest."
                    }
                },
                ["required"] = new JsonArray { "assignments", "rationale" }
            }
        };
}

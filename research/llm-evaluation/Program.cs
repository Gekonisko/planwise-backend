using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Estimates;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Infrastructure.Llm;

namespace PlanWise.Research.LlmEvaluation;

/// <summary>
/// Measures what the two LLM-backed mechanisms in PlanWise actually do, along the three axes of the
/// research question: conformance to the project data they were given, internal consistency of the
/// answer, and repeatability across identical calls.
///
/// No ground truth about real project spend is used. Every metric is either a property of the answer
/// on its own terms (does cost equal hours times rate?) or a comparison against the input the model
/// was handed (is this role in the rate card?) or against another run of the same input.
/// </summary>
internal static class Program
{
    private static readonly JsonSerializerOptions Output = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static async Task<int> Main(string[] args)
    {
        bool smoke = args.Contains("--smoke", StringComparer.Ordinal);
        int repetitions = IntArg(args, "--reps") ?? (smoke ? 1 : 5);
        int primaryRepetitions = IntArg(args, "--primary-reps") ?? (smoke ? 1 : 10);
        string outputDirectory = StringArg(args, "--out") ?? "runs";
        bool costOnly = args.Contains("--cost-only", StringComparer.Ordinal);
        string? only = StringArg(args, "--scenarios");
        var wanted = only?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool prioritisationOnly = args.Contains("--prio-only", StringComparer.Ordinal);

        // Allocation is opt-in. The two LLM arms of it are billed per turn, so a plain run of the
        // harness must not start spending money on them by accident.
        bool allocationOnly = args.Contains("--alloc-only", StringComparer.Ordinal);
        bool runAllocation = allocationOnly || args.Contains("--alloc", StringComparer.Ordinal);
        int agentTurns = IntArg(args, "--agent-turns") ?? 6;
        int costAgentTurns = IntArg(args, "--cost-agent-turns") ?? 4;
        bool runCost = !prioritisationOnly && !allocationOnly;
        bool runPrioritisation = !costOnly && !allocationOnly;

        IConfiguration configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var costOptions = Bind<PlanWise.Modules.CostEstimation.Infrastructure.Llm.AnthropicOptions>(
            configuration, PlanWise.Modules.CostEstimation.Infrastructure.Llm.AnthropicOptions.SectionName);
        var prioritisationOptions = Bind<PlanWise.Modules.BacklogPrioritisation.Infrastructure.Llm.AnthropicOptions>(
            configuration, PlanWise.Modules.BacklogPrioritisation.Infrastructure.Llm.AnthropicOptions.SectionName);

        if (string.IsNullOrWhiteSpace(costOptions.ApiKey) || string.IsNullOrWhiteSpace(prioritisationOptions.ApiKey))
        {
            Console.Error.WriteLine(
                "Brak klucza API. Ustaw CostEstimation:Anthropic:ApiKey oraz BacklogPrioritisation:Anthropic:ApiKey " +
                "w user secrets projektu API albo w zmiennych srodowiskowych.");
            return 2;
        }

        Directory.CreateDirectory(outputDirectory);
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        // Raw bodies are kept as evidence: every number reported later can be traced back to the
        // exact response it came from, and a deviation can be inspected rather than argued about.
        rawDirectory = Path.Combine(outputDirectory, "raw", stamp);
        Directory.CreateDirectory(rawDirectory);

        var recorder = new RecordingHandler();
        using var httpClient = new HttpClient(recorder, disposeHandler: false)
        {
            BaseAddress = new Uri(costOptions.BaseUrl),
            Timeout = TimeSpan.FromMinutes(5),
        };

        var costModel = Construct<ICostEstimationModel>(
            typeof(PlanWise.Modules.CostEstimation.Infrastructure.Llm.AnthropicOptions).Assembly,
            "PlanWise.Modules.CostEstimation.Infrastructure.Llm.AnthropicCostEstimationModel",
            httpClient,
            Options.Create(costOptions));

        // MaxTurns is the whole difference between the cost arms: the agentic class withholds its
        // checking tool at a budget of one and behaves as the shipped single-shot client does.
        var agenticCostOptions = new PlanWise.Modules.CostEstimation.Infrastructure.Llm.AnthropicOptions
        {
            ApiKey = costOptions.ApiKey,
            Model = costOptions.Model,
            BaseUrl = costOptions.BaseUrl,
            MaxTurns = costAgentTurns,
        };

        var agenticCostModel = Construct<ICostEstimationModel>(
            typeof(PlanWise.Modules.CostEstimation.Infrastructure.Llm.AnthropicOptions).Assembly,
            "PlanWise.Modules.CostEstimation.Infrastructure.Llm.AgenticCostEstimationModel",
            httpClient,
            Options.Create(agenticCostOptions),
            NullLoggerFor("PlanWise.Modules.CostEstimation.Infrastructure.Llm.AgenticCostEstimationModel",
                typeof(PlanWise.Modules.CostEstimation.Infrastructure.Llm.AnthropicOptions).Assembly));

        var prioritisationModel = Construct<IBacklogPrioritisationModel>(
            typeof(PlanWise.Modules.BacklogPrioritisation.Infrastructure.Llm.AnthropicOptions).Assembly,
            "PlanWise.Modules.BacklogPrioritisation.Infrastructure.Llm.AnthropicBacklogPrioritisationModel",
            httpClient,
            Options.Create(prioritisationOptions));

        var report = new EvaluationReport(
            StartedUtc: DateTime.UtcNow,
            CostModelName: costOptions.Model,
            PrioritisationModelName: prioritisationOptions.Model,
            Repetitions: repetitions,
            PrimaryRepetitions: primaryRepetitions,
            AgentTurnBudget: agentTurns,
            CostAgentTurnBudget: costAgentTurns,
            CostRuns: [],
            PrioritisationRuns: [],
            AllocationRuns: []);

        var costRuns = new List<CostRunRecord>();
        var prioritisationRuns = new List<PrioritisationRunRecord>();
        var allocationRuns = new List<AllocationRunner.AllocationRunRecord>();

        if (runCost)
        {
            foreach (CostScenarioDefinition scenario in Scenarios.CostScenarios(primaryRepetitions, repetitions))
            {
                if (wanted is not null && !wanted.Contains(scenario.Id))
                {
                    continue;
                }

                foreach ((ICostEstimationModel model, string arm) in new[]
                {
                    (costModel, "llm-single-shot"),
                    (agenticCostModel, "llm-agent"),
                })
                {
                    for (int attempt = 1; attempt <= scenario.Repetitions; attempt++)
                    {
                        Console.WriteLine($"[koszty] {scenario.Id} {arm} proba {attempt}/{scenario.Repetitions} ...");
                        CostRunRecord record = await RunCost(model, arm, recorder, scenario, attempt);
                        costRuns.Add(record);
                        await DumpRaw($"{scenario.Id}-{arm}", attempt, recorder);
                        await Save(outputDirectory, stamp, report with { CostRuns = costRuns, PrioritisationRuns = prioritisationRuns, AllocationRuns = allocationRuns });

                        Console.WriteLine(record.Metrics is null
                            ? $"          -> BLAD {record.Error}"
                            : $"          -> {record.HttpCalls} wywolan, {record.Usage.OutputTokens} tokenow wyjscia");
                    }
                }
            }
        }

        if (runPrioritisation)
        {
            foreach (PrioritisationScenarioDefinition scenario in Scenarios.PrioritisationScenarios(primaryRepetitions, repetitions))
            {
                if (wanted is not null && !wanted.Contains(scenario.Id))
                {
                    continue;
                }

                var edges = Scenarios.EdgesOf(scenario.Input);
                for (int attempt = 1; attempt <= scenario.Repetitions; attempt++)
                {
                    Console.WriteLine($"[priorytety] {scenario.Id} proba {attempt}/{scenario.Repetitions} ...");
                    prioritisationRuns.Add(await RunPrioritisation(prioritisationModel, recorder, scenario, edges, attempt));
                    await Save(outputDirectory, stamp, report with { CostRuns = costRuns, PrioritisationRuns = prioritisationRuns, AllocationRuns = allocationRuns });
                }
            }
        }

        if (runAllocation)
        {
            await RunAllocationSeries(
                configuration, costOptions.ApiKey, recorder, agentTurns, primaryRepetitions, repetitions, wanted,
                allocationRuns,
                async () => await Save(outputDirectory, stamp, report with
                {
                    CostRuns = costRuns,
                    PrioritisationRuns = prioritisationRuns,
                    AllocationRuns = allocationRuns,
                }));
        }

        string path = await Save(outputDirectory, stamp, report with
        {
            CostRuns = costRuns,
            PrioritisationRuns = prioritisationRuns,
            AllocationRuns = allocationRuns,
        });

        Console.WriteLine();
        Console.WriteLine($"Zapisano {costRuns.Count} przebiegow kosztowych, {prioritisationRuns.Count} priorytetyzacji i {allocationRuns.Count} alokacji -> {path}");
        Console.WriteLine(
            $"Nieudane: koszty {costRuns.Count(run => run.Error is not null)}, " +
            $"priorytety {prioritisationRuns.Count(run => run.Error is not null)}, " +
            $"alokacja {allocationRuns.Count(run => run.Error is not null)}");
        return 0;
    }

    private static async Task<CostRunRecord> RunCost(
        ICostEstimationModel model,
        string arm,
        RecordingHandler recorder,
        CostScenarioDefinition scenario,
        int attempt)
    {
        recorder.Clear();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            CostEstimateResult result = await model.EstimateAsync(scenario.Prompt);
            stopwatch.Stop();
            await DumpRaw(scenario.Id, attempt, recorder);
            Usage usage = Usage.From(recorder.Exchanges);

            return new CostRunRecord(
                scenario.Id,
                scenario.Description,
                arm,
                attempt,
                stopwatch.Elapsed.TotalSeconds,
                recorder.Exchanges.Count,
                usage,
                CostRunMetrics.Compute(scenario.Prompt, result),
                Result: result,
                Error: null);
        }
        // Deliberately catches everything. How a model answer fails is part of the result, and at
        // least one failure mode reaches here as an ArgumentNullException thrown inside the
        // production parser rather than as a JsonException its retry loop would have caught.
        catch (Exception exception)
        {
            stopwatch.Stop();
            await DumpRaw(scenario.Id, attempt, recorder);
            return new CostRunRecord(
                scenario.Id, scenario.Description, arm, attempt, stopwatch.Elapsed.TotalSeconds,
                recorder.Exchanges.Count, Usage.From(recorder.Exchanges), Metrics: null, Result: null,
                Error: $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static async Task<PrioritisationRunRecord> RunPrioritisation(
        IBacklogPrioritisationModel model,
        RecordingHandler recorder,
        PrioritisationScenarioDefinition scenario,
        IReadOnlyList<(string Successor, string Predecessor)> edges,
        int attempt)
    {
        recorder.Clear();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            PrioritisationResult result = await model.PrioritiseAsync(scenario.Input);
            stopwatch.Stop();
            await DumpRaw(scenario.Id, attempt, recorder);

            string lastBody = recorder.Exchanges.Count > 0 ? recorder.Exchanges[^1].ResponseBody : string.Empty;

            return new PrioritisationRunRecord(
                scenario.Id,
                scenario.Description,
                attempt,
                stopwatch.Elapsed.TotalSeconds,
                recorder.Exchanges.Count,
                Usage.From(recorder.Exchanges),
                PrioritisationRunMetrics.Compute(scenario.Input, result, lastBody, edges),
                Reasons: result.Ordered.Select(task => task.Reason).ToList(),
                Error: null);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            await DumpRaw(scenario.Id, attempt, recorder);
            return new PrioritisationRunRecord(
                scenario.Id, scenario.Description, attempt, stopwatch.Elapsed.TotalSeconds,
                recorder.Exchanges.Count, Usage.From(recorder.Exchanges), Metrics: null, Reasons: [],
                Error: $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    /// <summary>
    /// The allocation series: four methods over identical instances, every one of them scored by the
    /// same evaluator.
    ///
    /// The two deterministic arms run once each — re-running a deterministic algorithm restates its
    /// own answer, and what they contribute here is the reference point rather than a distribution.
    /// The two LLM arms are repeated, because dispersion across identical calls is exactly what the
    /// earlier cost-estimation series found to be the interesting variable.
    ///
    /// Single-shot and agentic are the same class with a different turn budget, constructed side by
    /// side here so it is visible that nothing else about them differs.
    /// </summary>
    private static async Task RunAllocationSeries(
        IConfiguration configuration,
        string fallbackApiKey,
        RecordingHandler recorder,
        int agentTurns,
        int primaryRepetitions,
        int repetitions,
        HashSet<string>? wanted,
        List<AllocationRunner.AllocationRunRecord> runs,
        Func<Task> save)
    {
        var bound = Bind<AnthropicSchedulingOptions>(configuration, AnthropicSchedulingOptions.SectionName);

        // Scheduling has no key of its own in a stock install, and setting a second secret for a
        // research run buys nothing — same provider, same account. Stated here rather than hidden.
        string apiKey = string.IsNullOrWhiteSpace(bound.ApiKey) ? fallbackApiKey : bound.ApiKey;

        AnthropicSchedulingOptions WithTurns(int turns) => new()
        {
            ApiKey = apiKey,
            Model = bound.Model,
            BaseUrl = bound.BaseUrl,
            MaxTurns = turns,
        };

        using var client = new HttpClient(recorder, disposeHandler: false)
        {
            BaseAddress = new Uri(bound.BaseUrl),
            // An agent run is several full model calls in sequence, so the ceiling has to cover the
            // whole loop rather than one turn of it.
            Timeout = TimeSpan.FromMinutes(15),
        };

        Console.WriteLine($"[alokacja] model {bound.Model}, budzet agenta {agentTurns} tur");

        foreach (AllocationScenarios.AllocationScenarioDefinition scenario in AllocationScenarios.All(primaryRepetitions, repetitions))
        {
            if (wanted is not null && !wanted.Contains(scenario.Id))
            {
                continue;
            }

            long reference = await AllocationRunner.ReferenceMakespanAsync(scenario.Input);
            Console.WriteLine($"[alokacja] {scenario.Id}: optimum CP-SAT = {reference} dni");

            foreach ((IScheduleOptimisationModel model, string arm) in new (IScheduleOptimisationModel, string)[]
            {
                (AllocationRunner.CpSat(), AllocationRunner.ArmCpSat),
                (AllocationRunner.Greedy(), AllocationRunner.ArmGreedy),
            })
            {
                runs.Add(await AllocationRunner.RunAsync(model, arm, recorder, scenario, 1, reference));
                await save();
            }

            foreach ((int turns, string arm) in new[] { (1, AllocationRunner.ArmSingleShot), (agentTurns, AllocationRunner.ArmAgent) })
            {
                IScheduleOptimisationModel model = AllocationRunner.Agent(client, WithTurns(turns));

                for (int attempt = 1; attempt <= scenario.Repetitions; attempt++)
                {
                    Console.WriteLine($"[alokacja] {scenario.Id} {arm} proba {attempt}/{scenario.Repetitions} ...");
                    AllocationRunner.AllocationRunRecord record =
                        await AllocationRunner.RunAsync(model, arm, recorder, scenario, attempt, reference);
                    runs.Add(record);
                    await DumpRaw($"{scenario.Id}-{arm}", attempt, recorder);
                    await save();

                    if (record.ApiError is { } apiError)
                    {
                        // Credit exhausted, bad key, rate limit: every further run would be the
                        // greedy fallback wearing an LLM label. Stop rather than fill the report
                        // with rows that read as measurements.
                        Console.Error.WriteLine();
                        Console.Error.WriteLine($"PRZERWANO SERIE — dostawca odrzucil wywolanie: {apiError[..Math.Min(300, apiError.Length)]}");
                        Console.Error.WriteLine("Dotychczasowe przebiegi sa zapisane; wznow przez --scenarios <lista>.");
                        return;
                    }

                    if (record.Metrics is { } metrics)
                    {
                        string gap = metrics.GapPercent is double value
                            ? $"+{value:0.0}% nad optimum"
                            : $"NIEKOMPLETNA, {metrics.OpenTasksLeftUnassigned} zadan bez wlasciciela";
                        Console.WriteLine(
                            $"          -> {metrics.MakespanDays} dni ({gap}), " +
                            $"{record.HttpCalls} wywolan, {record.Usage.OutputTokens} tokenow wyjscia");
                    }
                    else
                    {
                        Console.WriteLine($"          -> BLAD {record.Error}");
                    }
                }
            }
        }
    }

    private static string rawDirectory = string.Empty;

    /// <summary>Writes every captured exchange of one run to disk, so a reported metric can be re-checked against its source.</summary>
    private static async Task DumpRaw(string scenarioId, int attempt, RecordingHandler recorder)
    {
        for (int i = 0; i < recorder.Exchanges.Count; i++)
        {
            RecordingHandler.Exchange exchange = recorder.Exchanges[i];
            string suffix = recorder.Exchanges.Count > 1 ? $"-call{i + 1}" : string.Empty;
            await File.WriteAllTextAsync(
                Path.Combine(rawDirectory, $"{scenarioId}-{attempt:D2}{suffix}.response.json"),
                exchange.ResponseBody);

            if (attempt == 1 && i == 0)
            {
                // One copy of the request per scenario is enough to document exactly what was sent.
                await File.WriteAllTextAsync(
                    Path.Combine(rawDirectory, $"{scenarioId}.request.json"),
                    exchange.RequestBody);
            }
        }
    }

    private static async Task<string> Save(string directory, string stamp, EvaluationReport report)
    {
        string path = Path.Combine(directory, $"llm-evaluation-{stamp}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, Output));
        return path;
    }

    private static TOptions Bind<TOptions>(IConfiguration configuration, string section)
        where TOptions : class, new()
    {
        var options = new TOptions();
        configuration.GetSection(section).Bind(options);
        return options;
    }

    /// <summary>
    /// Both Anthropic clients are internal to their Infrastructure assemblies. Reflection keeps the
    /// harness out of production code entirely — the alternative, an InternalsVisibleTo attribute,
    /// would mean editing shipped assemblies to accommodate a measurement tool.
    /// </summary>
    /// <summary>
    /// A no-op ILogger&lt;T&gt; for a type the harness can only name, not reference. The non-generic
    /// NullLogger.Instance will not do: the constructors want ILogger&lt;T&gt; and the reflection
    /// binder matches on the exact parameter type. NullLogger&lt;T&gt;.Instance is a static field,
    /// unlike its non-generic namesake's property, hence both lookups.
    /// </summary>
    private static object NullLoggerFor(string typeName, Assembly assembly)
    {
        Type target = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Nie znaleziono typu {typeName}.");
        Type loggerType = typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>).MakeGenericType(target);

        return loggerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? loggerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? Activator.CreateInstance(loggerType, nonPublic: true)!;
    }

    private static T Construct<T>(Assembly assembly, string typeName, params object[] arguments)
    {
        Type type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Nie znaleziono typu {typeName} w {assembly.GetName().Name}.");

        return (T)(Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: arguments,
            culture: null)
            ?? throw new InvalidOperationException($"Nie udalo sie utworzyc {typeName}."));
    }

    private static int? IntArg(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], CultureInfo.InvariantCulture, out int value)
            ? value
            : null;
    }

    private static string? StringArg(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}

internal sealed record Usage(int InputTokens, int OutputTokens, int Calls)
{
    public static Usage From(IReadOnlyList<RecordingHandler.Exchange> exchanges)
    {
        int input = 0;
        int output = 0;

        foreach (RecordingHandler.Exchange exchange in exchanges)
        {
            try
            {
                using var document = JsonDocument.Parse(exchange.ResponseBody);
                if (document.RootElement.TryGetProperty("usage", out JsonElement usage))
                {
                    if (usage.TryGetProperty("input_tokens", out JsonElement inputTokens))
                    {
                        input += inputTokens.GetInt32();
                    }

                    if (usage.TryGetProperty("output_tokens", out JsonElement outputTokens))
                    {
                        output += outputTokens.GetInt32();
                    }
                }
            }
            catch (JsonException)
            {
                // A malformed body still counts as a call; token usage for it is simply unknown.
            }
        }

        return new Usage(input, output, exchanges.Count);
    }
}

internal sealed record CostRunRecord(
    string ScenarioId,
    string Scenario,
    string Arm,
    int Attempt,
    double ElapsedSeconds,
    int HttpCalls,
    Usage Usage,
    CostRunMetrics? Metrics,
    CostEstimateResult? Result,
    string? Error);

internal sealed record PrioritisationRunRecord(
    string ScenarioId,
    string Scenario,
    int Attempt,
    double ElapsedSeconds,
    int HttpCalls,
    Usage Usage,
    PrioritisationRunMetrics? Metrics,
    IReadOnlyList<string> Reasons,
    string? Error);

internal sealed record EvaluationReport(
    DateTime StartedUtc,
    string CostModelName,
    string PrioritisationModelName,
    int Repetitions,
    int PrimaryRepetitions,
    int AgentTurnBudget,
    int CostAgentTurnBudget,
    IReadOnlyList<CostRunRecord> CostRuns,
    IReadOnlyList<PrioritisationRunRecord> PrioritisationRuns,
    IReadOnlyList<AllocationRunner.AllocationRunRecord> AllocationRuns);

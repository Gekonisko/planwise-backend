using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions;

namespace PlanWise.Modules.BacklogPrioritisation.Infrastructure.Llm;

// Orders the backlog with an LLM instead of the weighted scorecard. Ranking a backlog is a
// judgement call with no ground truth to fit against, which is exactly the shape of problem an LLM
// suits and a trained model does not — see IBacklogPrioritisationModel for that reasoning.
//
// Same transport approach as AnthropicCostEstimationModel: the Messages API over plain HttpClient
// (no SDK dependency), with a forced tool call rather than prose JSON because a tool call is far
// more reliable to parse.
internal sealed class AnthropicBacklogPrioritisationModel(HttpClient httpClient, IOptions<AnthropicOptions> options)
    : IBacklogPrioritisationModel
{
    private const string ToolName = "submit_backlog_order";
    private const int MaxAttempts = 2;

    // Server-side fallback: if a safety classifier declines the request, the API re-routes to a
    // suitable fallback model instead of handing back an unusable turn. "default" routes by refusal
    // category, so there is no model list here to keep up to date.
    private const string FallbackBeta = "server-side-fallback-2026-07-01";

    /// <summary>A prioritisation with no risk forecast behind it treats every task as mid-risk.</summary>
    private const decimal NeutralRiskScore = 0.5m;

    private static readonly JsonSerializerOptions ResultSerializerOptions = new(JsonSerializerDefaults.Web);

    public string ModelName => options.Value.Model;

    public async Task<PrioritisationResult> PrioritiseAsync(
        PrioritisationInput input,
        CancellationToken cancellationToken = default)
    {
        // Nothing to order, and no reason to spend tokens discovering that.
        if (input.BacklogTasks.Count == 0)
        {
            return new PrioritisationResult([]);
        }

        // Tasks are identified to the model by their short human key (PW-14), never by GUID: models
        // reproduce short opaque strings reliably and long ones badly.
        var tasksByKey = input.BacklogTasks.ToDictionary(task => task.Key, StringComparer.OrdinalIgnoreCase);

        JsonException? lastParseFailure = null;
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            string responseBody = await SendRequestAsync(input, cancellationToken);

            try
            {
                IReadOnlyList<RankedTask> ranked = ParseResult(responseBody);
                return BuildResult(ranked, input, tasksByKey);
            }
            catch (JsonException exception)
            {
                // A schema deviation is worth one retry; a transport or auth failure is not, and
                // SendRequestAsync throws those straight out of the loop.
                lastParseFailure = exception;
            }
        }

        throw new InvalidOperationException(
            $"Anthropic tool_use response did not match the expected schema after {MaxAttempts} attempts", lastParseFailure);
    }

    // Maps the model's answer back onto real tasks. Three things are enforced here rather than
    // trusted from the response: unknown keys are dropped, a task named twice is kept only the first
    // time, and anything the model failed to mention is appended in its existing backlog order — a
    // prioritisation that silently loses rows would look to the user like tasks had disappeared.
    private static PrioritisationResult BuildResult(
        IReadOnlyList<RankedTask> ranked,
        PrioritisationInput input,
        Dictionary<string, TaskInsightSummary> tasksByKey)
    {
        var ordered = new List<PrioritisedTask>(input.BacklogTasks.Count);
        var seen = new HashSet<Guid>();

        foreach (RankedTask entry in ranked)
        {
            if (entry.TaskKey is null ||
                !tasksByKey.TryGetValue(entry.TaskKey, out TaskInsightSummary? task) ||
                !seen.Add(task.TaskId))
            {
                continue;
            }

            ordered.Add(ToPrioritisedTask(task, entry, input.RiskScores));
        }

        foreach (TaskInsightSummary task in input.BacklogTasks.Where(task => !seen.Contains(task.TaskId)))
        {
            ordered.Add(ToPrioritisedTask(task, Unranked, input.RiskScores));
        }

        return new PrioritisationResult(ordered);
    }

    private static readonly RankedTask Unranked = new(null, 0.5m, 0.5m, 0.5m, "Not ranked by the model; left in its existing backlog position");

    // riskScore is taken from RiskPrediction's own output, never from the model: the model is told
    // the risk figures so it can weigh them when ordering, but the number persisted and shown in the
    // UI stays the one the risk module actually produced.
    private static PrioritisedTask ToPrioritisedTask(
        TaskInsightSummary task,
        RankedTask entry,
        IReadOnlyDictionary<Guid, decimal> riskScores) =>
        new(
            task.TaskId,
            task.Key,
            Clamp(entry.ValueScore),
            Clamp(entry.DependencyScore),
            Clamp(entry.ComplexityScore),
            riskScores.TryGetValue(task.TaskId, out decimal risk) ? risk : NeutralRiskScore,
            string.IsNullOrWhiteSpace(entry.Reason) ? "No reason given" : entry.Reason.Trim());

    private static decimal Clamp(decimal score) => Math.Clamp(score, 0m, 1m);

    private async Task<string> SendRequestAsync(PrioritisationInput input, CancellationToken cancellationToken)
    {
        JsonObject requestBody = BuildRequestBody(input);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", options.Value.ApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Add("anthropic-beta", FallbackBeta);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Anthropic API request failed with status {(int)response.StatusCode}: {responseBody}");
        }

        return responseBody;
    }

    private JsonObject BuildRequestBody(PrioritisationInput input) =>
        new()
        {
            ["model"] = options.Value.Model,
            ["max_tokens"] = 16000,
            // thinking is deliberately omitted rather than set: on Opus-5-class models an absent
            // thinking parameter already runs adaptive thinking, and leaving it absent avoids
            // pinning behaviour that differs across model generations.
            ["fallbacks"] = "default",
            ["system"] = "You order a software project's product backlog so the team delivers the most value soonest.\n\n" +
                         "Weigh, in roughly this order of importance:\n" +
                         "- Business value, read from both the stated value figure and what the task's title implies.\n" +
                         "- Unblocking: a task that other tasks wait on should come before the work it blocks.\n" +
                         "- Slip risk, which is supplied per task — pull genuinely risky work earlier so it fails early rather than late.\n" +
                         "- Effort, as a tie-breaker only: prefer the cheaper of two otherwise comparable items.\n\n" +
                         "Hard constraints:\n" +
                         "- Return every task you were given, exactly once, identified by its key exactly as written.\n" +
                         "- Never invent a task key that was not in the input.\n" +
                         "- The order of the array is the proposed backlog order, most important first.\n" +
                         "- valueScore, dependencyScore and complexityScore are each between 0 and 1, and describe the " +
                         "task relative to the rest of this backlog rather than on any absolute scale. complexityScore " +
                         "means how large or involved the task is, where 1 is the most complex.\n" +
                         "- reason is one short clause naming the factors that actually decided the position. Make it " +
                         "specific to the task; do not restate the scores.\n\n" +
                         "Always call the submit_backlog_order tool with your answer.",
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = BuildUserMessage(input)
                }
            },
            ["tools"] = new JsonArray { BuildToolDefinition() },
            ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = ToolName }
        };

    private static string BuildUserMessage(PrioritisationInput input)
    {
        // Predecessors are referenced by key, not GUID, so the model can actually follow a chain.
        var keysByTaskId = input.BacklogTasks.ToDictionary(task => task.TaskId, task => task.Key);

        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Backlog to order ({input.BacklogTasks.Count} tasks), in its current order:");
        builder.AppendLine();

        foreach (TaskInsightSummary task in input.BacklogTasks)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- [{task.Key}] {task.Title}");
            builder.AppendLine(CultureInfo.InvariantCulture, $"    priority: {task.Priority}");
            builder.AppendLine(CultureInfo.InvariantCulture,
                $"    business value: {Describe(task.BusinessValue, "not set")}    estimate: {Describe(task.Points, "unestimated")} points");

            if (task.DueDate is DateOnly dueDate)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"    due: {dueDate:yyyy-MM-dd}");
            }

            builder.AppendLine(CultureInfo.InvariantCulture, $"    blocks {task.BlocksCount} other task(s)");

            IEnumerable<string> predecessorKeys = task.PredecessorTaskIds
                .Where(keysByTaskId.ContainsKey)
                .Select(id => keysByTaskId[id]);
            string predecessorText = string.Join(", ", predecessorKeys);
            if (predecessorText.Length > 0)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"    waits on: {predecessorText}");
            }

            if (task.SubtaskTotal > 0)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"    subtasks: {task.SubtaskDone}/{task.SubtaskTotal} done");
            }

            if (input.RiskScores.TryGetValue(task.TaskId, out decimal risk))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"    forecast slip risk: {risk:P0}");
            }
        }

        if (input.RiskScores.Count == 0)
        {
            builder.AppendLine();
            builder.AppendLine("No slip-risk forecast has been run for this project, so no risk figures are given — " +
                               "order on the remaining factors and do not speculate about risk.");
        }

        return builder.ToString();
    }

    private static string Describe(int? value, string absent) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? absent;

    private static JsonObject BuildToolDefinition() =>
        new()
        {
            ["name"] = ToolName,
            ["description"] = "Submit the proposed backlog order, most important task first.",
            ["input_schema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["order"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["description"] = "Every task from the input, exactly once, most important first.",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["taskKey"] = new JsonObject { ["type"] = "string" },
                                ["valueScore"] = new JsonObject { ["type"] = "number" },
                                ["dependencyScore"] = new JsonObject { ["type"] = "number" },
                                ["complexityScore"] = new JsonObject { ["type"] = "number" },
                                ["reason"] = new JsonObject { ["type"] = "string" }
                            },
                            ["required"] = new JsonArray { "taskKey", "valueScore", "dependencyScore", "complexityScore", "reason" }
                        }
                    }
                },
                ["required"] = new JsonArray { "order" }
            }
        };

    private static IReadOnlyList<RankedTask> ParseResult(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        JsonElement root = document.RootElement;

        // A refusal comes back as a successful HTTP 200 with no tool call, so stop_reason has to be
        // checked before the content is read. With server-side fallbacks enabled this should be rare
        // — it means the fallback declined too.
        if (root.TryGetProperty("stop_reason", out JsonElement stopReason) && stopReason.GetString() == "refusal")
        {
            string category = root.TryGetProperty("stop_details", out JsonElement details) &&
                              details.TryGetProperty("category", out JsonElement categoryElement)
                ? categoryElement.GetString() ?? "unspecified"
                : "unspecified";
            throw new InvalidOperationException($"Anthropic declined the prioritisation request (refusal category: {category})");
        }

        foreach (JsonElement block in root.GetProperty("content").EnumerateArray())
        {
            if (block.GetProperty("type").GetString() != "tool_use")
            {
                continue;
            }

            ToolResult toolResult = block.GetProperty("input").Deserialize<ToolResult>(ResultSerializerOptions)
                ?? throw new InvalidOperationException("Anthropic tool_use input could not be deserialized into a ToolResult");
            return toolResult.Order;
        }

        throw new InvalidOperationException("Anthropic response did not contain a tool_use block");
    }

    // Mirrors the tool_use input_schema exactly. There is no riskScore here on purpose — that number
    // comes from RiskPrediction, not from the model.
    private sealed record ToolResult(IReadOnlyList<RankedTask> Order);

    private sealed record RankedTask(
        string? TaskKey,
        decimal ValueScore,
        decimal DependencyScore,
        decimal ComplexityScore,
        string? Reason);
}

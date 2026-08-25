using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Estimates;

namespace PlanWise.Modules.CostEstimation.Infrastructure.Llm;

// Calls the Anthropic Messages API directly over HttpClient (no third-party SDK dependency). Forces
// structured output via tool-use rather than asking for prose JSON, since a forced tool call is far
// more reliable to parse than hoping a text response is valid JSON. The tool's input_schema uses
// camelCase to match CostEstimateResult's own JSON casing (JsonSerializerDefaults.Web) — everything
// else in the request envelope (model, max_tokens, tool_choice, ...) uses Anthropic's own snake_case
// field names, which is why this is built with JsonNode rather than one typed/naming-policy'd DTO.
internal sealed class AnthropicCostEstimationModel(HttpClient httpClient, IOptions<AnthropicOptions> options) : ICostEstimationModel
{
    private const string ToolName = "submit_cost_estimate";
    private static readonly JsonSerializerOptions ResultSerializerOptions = new(JsonSerializerDefaults.Web);

    private const int MaxAttempts = 2;

    public string ModelName => options.Value.Model;

    // A forced tool call is far more reliable than prose JSON, but not perfectly so — the model
    // occasionally returns a value that doesn't strictly match input_schema (e.g. a string where an
    // array was declared). That's a schema deviation, not a transport failure, so it's worth one
    // retry before surfacing it as a real job failure; an HTTP-level failure (bad auth, etc.) is not
    // retried since it will just fail identically.
    public async Task<CostEstimateResult> EstimateAsync(CostEstimationPrompt prompt, CancellationToken cancellationToken = default)
    {
        JsonException? lastParseFailure = null;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            string responseBody = await SendRequestAsync(prompt, cancellationToken);

            try
            {
                return ParseResult(responseBody);
            }
            catch (JsonException exception)
            {
                lastParseFailure = exception;
            }
        }

        throw new InvalidOperationException(
            $"Anthropic tool_use response did not match the expected schema after {MaxAttempts} attempts", lastParseFailure);
    }

    private async Task<string> SendRequestAsync(CostEstimationPrompt prompt, CancellationToken cancellationToken)
    {
        JsonObject requestBody = BuildRequestBody(prompt);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
        {
            Content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json")
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

        return responseBody;
    }

    private JsonObject BuildRequestBody(CostEstimationPrompt prompt)
    {
        return new JsonObject
        {
            ["model"] = options.Value.Model,
            ["max_tokens"] = 4096,
            ["system"] = "You are a cost estimation assistant for software delivery projects. Produce a realistic, " +
                         "well-reasoned cost estimate from the backlog and rate card provided, plus a short list of " +
                         "concrete cost-reduction recommendations (e.g. descoping a low-priority item, reducing a " +
                         "role's allocated hours, deferring non-labour spend) each with an estimated dollar saving. " +
                         "Always call the submit_cost_estimate tool with your answer.",
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = BuildUserMessage(prompt)
                }
            },
            ["tools"] = new JsonArray { BuildToolDefinition() },
            ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = ToolName }
        };
    }

    private static string BuildUserMessage(CostEstimationPrompt prompt)
    {
        var builder = new StringBuilder();
        string clientSuffix = prompt.ClientName is null ? string.Empty : $" (client: {prompt.ClientName})";
        builder.AppendLine(CultureInfo.InvariantCulture, $"Project: {prompt.ProjectName}{clientSuffix}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Currency: {prompt.Currency}");
        builder.AppendLine();
        builder.AppendLine("Role rate card:");
        foreach (RoleRate rate in prompt.RateCard)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- {rate.Role}: {rate.HourlyRate} {rate.Currency}/hour");
        }

        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Backlog ({prompt.Tasks.Count} not-yet-done tasks):");
        foreach (CostEstimationTaskSummary task in prompt.Tasks)
        {
            string points = task.Points?.ToString(CultureInfo.InvariantCulture) ?? "unestimated";
            builder.AppendLine(CultureInfo.InvariantCulture, $"- [{task.Key}] {task.Title} (priority: {task.Priority}, points: {points})");
            if (!string.IsNullOrWhiteSpace(task.Description))
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  {task.Description}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("No historical actuals (real spend data) are available yet for this project — note that " +
                            "explicitly as an assumption rather than inventing figures.");

        return builder.ToString();
    }

    private static JsonObject BuildToolDefinition() =>
        new()
        {
            ["name"] = ToolName,
            ["description"] = "Submit the structured cost estimate for the project.",
            ["input_schema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["scenarios"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["name"] = new JsonObject { ["type"] = "string" },
                                ["percentile"] = new JsonObject { ["type"] = "integer" },
                                ["total"] = new JsonObject { ["type"] = "number" },
                                ["confidence"] = new JsonObject { ["type"] = "string" }
                            },
                            ["required"] = new JsonArray { "name", "percentile", "total", "confidence" }
                        }
                    },
                    ["labourLines"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["role"] = new JsonObject { ["type"] = "string" },
                                ["hours"] = new JsonObject { ["type"] = "number" },
                                ["hourlyRate"] = new JsonObject { ["type"] = "number" },
                                ["cost"] = new JsonObject { ["type"] = "number" }
                            },
                            ["required"] = new JsonArray { "role", "hours", "hourlyRate", "cost" }
                        }
                    },
                    ["nonLabourLines"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["description"] = new JsonObject { ["type"] = "string" },
                                ["amount"] = new JsonObject { ["type"] = "number" }
                            },
                            ["required"] = new JsonArray { "description", "amount" }
                        }
                    },
                    ["priorityBreakdown"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["priority"] = new JsonObject { ["type"] = "string" },
                                ["total"] = new JsonObject { ["type"] = "number" }
                            },
                            ["required"] = new JsonArray { "priority", "total" }
                        }
                    },
                    ["assumptions"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject { ["type"] = "string" }
                    },
                    ["reasoning"] = new JsonObject { ["type"] = "string" },
                    ["reductions"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["items"] = new JsonObject
                        {
                            ["type"] = "object",
                            ["properties"] = new JsonObject
                            {
                                ["description"] = new JsonObject { ["type"] = "string" },
                                ["saving"] = new JsonObject { ["type"] = "number" },
                                ["effect"] = new JsonObject { ["type"] = "string" },
                                ["confidence"] = new JsonObject { ["type"] = "string" }
                            },
                            ["required"] = new JsonArray { "description", "saving", "effect", "confidence" }
                        }
                    }
                },
                ["required"] = new JsonArray { "scenarios", "labourLines", "nonLabourLines", "priorityBreakdown", "assumptions", "reasoning", "reductions" }
            }
        };

    private static CostEstimateResult ParseResult(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);

        foreach (JsonElement block in document.RootElement.GetProperty("content").EnumerateArray())
        {
            if (block.GetProperty("type").GetString() == "tool_use")
            {
                JsonElement input = block.GetProperty("input");
                ToolResult toolResult = input.Deserialize<ToolResult>(ResultSerializerOptions)
                    ?? throw new InvalidOperationException("Anthropic tool_use input could not be deserialized into a ToolResult");

                IReadOnlyList<CostReduction> reductions = toolResult.Reductions
                    .Select(candidate => new CostReduction(Guid.NewGuid(), candidate.Description, candidate.Saving, candidate.Effect, candidate.Confidence))
                    .ToList();

                return new CostEstimateResult(
                    toolResult.Scenarios,
                    toolResult.LabourLines,
                    toolResult.NonLabourLines,
                    toolResult.PriorityBreakdown,
                    toolResult.Assumptions,
                    toolResult.Reasoning,
                    reductions);
            }
        }

        throw new InvalidOperationException("Anthropic response did not contain a tool_use block");
    }

    // Mirrors the tool_use input_schema exactly. Reductions arrive without an id — the model isn't
    // reliable at producing valid, stable identifiers, so ParseResult assigns a fresh Guid per item
    // after deserializing into this intermediate shape.
    private sealed record ToolResult(
        IReadOnlyList<CostScenario> Scenarios,
        IReadOnlyList<LabourLine> LabourLines,
        IReadOnlyList<NonLabourLine> NonLabourLines,
        IReadOnlyList<PriorityCostLine> PriorityBreakdown,
        IReadOnlyList<string> Assumptions,
        string Reasoning,
        IReadOnlyList<ReductionCandidate> Reductions);

    private sealed record ReductionCandidate(string Description, decimal Saving, string Effect, string Confidence);
}

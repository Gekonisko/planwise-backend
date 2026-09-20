using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanWise.Modules.CostEstimation.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Estimates;

namespace PlanWise.Modules.CostEstimation.Infrastructure.Llm;

// The agentic counterpart to AnthropicCostEstimationModel, and its controlled comparison.
//
// SAME QUESTION, ONE DIFFERENCE. It sends the same system prompt, the same user message and the same
// submit_cost_estimate schema — all three taken from AnthropicCostEstimationModel rather than copied,
// so they cannot drift apart. The single difference is check_estimate: a tool that reconciles a draft
// against the rate card and against its own line items and hands the findings back, letting the model
// correct itself before committing. At MaxTurns = 1 the tool is withheld and this class reproduces the
// single-shot arm exactly.
//
// WHAT IT IS AIMED AT. A measured series of the single-shot model found conformance flawless and
// consistency poor: 4 of 26 runs returned no cost scenario at all, only 7 of 22 had a scenario that
// reconciled with the sum of its own line items, and 4 of 89 labour lines broke cost = hours x rate —
// every one of them on the single role whose blended rate was not a round number. None of those is a
// judgement call. They are all arithmetic, and arithmetic is exactly what a tool can check and a
// language model reliably cannot. Whether a checking loop actually repairs them is the experiment.
//
// WHAT WAS EXPECTED, AND WHAT HAPPENED. The same series found the estimate's *magnitude* unstable —
// the same eight-task backlog costed between 58k and 211k USD across identical calls. The expectation
// written here before measuring was that reconciliation would fix consistency and leave dispersion
// roughly where it was, since checking arithmetic says nothing about whether the hours were plausible.
//
// The first half held: reconciliation went from 4 of 22 runs to 22 of 22, and the best variant's
// deviation from its own line items fell to zero on every run. The second half did not. Dispersion of
// the total fell too — coefficient of variation 0.337 to 0.095, 0.357 to 0.107 and 0.554 to 0.063 on
// three of the four scenarios. The prediction recorded here was wrong and is left in place rather than
// quietly corrected.
//
// A plausible and so far unverified explanation: forced to reconcile, the model anchors the headline
// figure to the sum of its own line items, and that sum moves less than a figure chosen freely. Note
// what is still not claimed — nothing here shows the estimates became *accurate*. There is no ground
// truth in this study, and a stable wrong number is still wrong.
internal sealed class AgenticCostEstimationModel(
    HttpClient httpClient,
    IOptions<AnthropicOptions> options,
    ILogger<AgenticCostEstimationModel> logger) : ICostEstimationModel
{
    private const string CheckTool = "check_estimate";

    public string ModelName => $"{options.Value.Model} (agentic, {Math.Max(1, options.Value.MaxTurns)} turns)";

    public async Task<CostEstimateResult> EstimateAsync(CostEstimationPrompt prompt, CancellationToken cancellationToken = default)
    {
        var messages = new JsonArray
        {
            new JsonObject
            {
                ["role"] = "user",
                ["content"] = AnthropicCostEstimationModel.BuildUserMessage(prompt)
            }
        };

        int maxTurns = Math.Max(1, options.Value.MaxTurns);
        JsonException? lastParseFailure = null;

        for (int turn = 1; turn <= maxTurns; turn++)
        {
            // The final turn withholds check_estimate and forces the submission. A check returned on
            // the last turn could never be acted on, and offering a tool that cannot help is how a
            // forced submission ends up empty.
            bool lastTurn = turn == maxTurns;
            JsonNode response = await SendAsync(messages, lastTurn, cancellationToken);

            JsonArray content = AsArrayOrNull(response["content"])
                ?? throw new JsonException("Anthropic response contained no content block");

            messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = content.DeepClone() });

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

                if (toolName == AnthropicCostEstimationModel.ToolName)
                {
                    try
                    {
                        using var document = JsonDocument.Parse((toolInput ?? new JsonObject()).ToJsonString());
                        return AnthropicCostEstimationModel.ParseToolInput(document.RootElement);
                    }
                    catch (JsonException exception)
                    {
                        // A deviation on a non-final turn is worth another turn rather than another
                        // whole request: the model is still in the loop and can be told what broke.
                        lastParseFailure = exception;
                        if (lastTurn)
                        {
                            break;
                        }

                        toolResults.Add(new JsonObject
                        {
                            ["type"] = "tool_result",
                            ["tool_use_id"] = toolUseId,
                            ["is_error"] = true,
                            ["content"] = $"That submission could not be accepted: {exception.Message}. Correct it and submit again."
                        });
                    }
                }
                else if (toolName == CheckTool)
                {
                    toolResults.Add(new JsonObject
                    {
                        ["type"] = "tool_result",
                        ["tool_use_id"] = toolUseId,
                        ["content"] = Reconcile(toolInput, prompt)
                    });
                }
            }

            if (toolResults.Count == 0)
            {
                break;
            }

            messages.Add(new JsonObject { ["role"] = "user", ["content"] = toolResults });
        }

        logger.LogWarning("The cost estimation agent produced no usable estimate within {Turns} turn(s).", maxTurns);
        throw new InvalidOperationException(
            $"The cost estimation agent did not submit a usable estimate within {maxTurns} turn(s)", lastParseFailure);
    }

    private async Task<JsonNode> SendAsync(
        JsonArray messages, bool forceSubmit, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["model"] = options.Value.Model,
            ["max_tokens"] = 8192,
            ["system"] = forceSubmit ? AnthropicCostEstimationModel.SystemPrompt : SystemPromptWithChecking,
            ["messages"] = messages.DeepClone(),
            ["tools"] = forceSubmit
                ? new JsonArray { AnthropicCostEstimationModel.BuildToolDefinition() }
                : new JsonArray { CheckToolDefinition(), AnthropicCostEstimationModel.BuildToolDefinition() },
            ["tool_choice"] = forceSubmit
                ? new JsonObject { ["type"] = "tool", ["name"] = AnthropicCostEstimationModel.ToolName }
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

    /// <summary>
    /// The base prompt plus the loop's own instructions. Appended rather than rewritten so the task
    /// definition stays byte-identical to the single-shot arm's.
    /// </summary>
    private const string SystemPromptWithChecking =
        AnthropicCostEstimationModel.SystemPrompt +
        "\n\nBefore you submit, call check_estimate with your draft. It recomputes every line against " +
        "the rate card and tells you: which roles are not on the rate card, which rates you changed, " +
        "which lines break cost = hours x rate, what your line items actually add up to, and how far " +
        "each scenario total is from that sum. Fix everything it reports, then submit. A scenario " +
        "total that does not reconcile with your own line items is an error, not a judgement call.";

    /// <summary>
    /// Recomputes a draft and reports what is wrong with it. Arithmetic only — it never suggests an
    /// hours figure, because the size of the estimate is the model's judgement and the study is about
    /// what happens when only the checkable part is checked.
    /// </summary>
    private static string Reconcile(JsonNode? draft, CostEstimationPrompt prompt)
    {
        var rateCard = prompt.RateCard.ToDictionary(rate => rate.Role, rate => rate.HourlyRate, StringComparer.OrdinalIgnoreCase);
        var builder = new StringBuilder();
        var problems = new List<string>();

        decimal labourTotal = 0;
        JsonArray labour = AsArrayOrNull(draft?["labourLines"]) ?? [];
        if (draft?["labourLines"] is not null && AsArrayOrNull(draft["labourLines"]) is null)
        {
            problems.Add("labourLines must be an array of objects, not a single value. Resend it as a list.");
        }

        foreach (JsonNode? line in labour)
        {
            string role = line?["role"]?.GetValue<string>() ?? "(missing role)";
            decimal hours = Decimal(line?["hours"]);
            decimal rate = Decimal(line?["hourlyRate"]);
            decimal cost = Decimal(line?["cost"]);
            labourTotal += cost;

            if (!rateCard.TryGetValue(role, out decimal expectedRate))
            {
                problems.Add($"Role '{role}' is not on the rate card. Allowed roles: {string.Join(", ", rateCard.Keys)}.");
                continue;
            }

            if (rate != expectedRate)
            {
                problems.Add($"Role '{role}': you used a rate of {rate}, but the rate card says {expectedRate}.");
            }

            decimal expectedCost = decimal.Round(hours * expectedRate, 2, MidpointRounding.AwayFromZero);
            if (Math.Abs(cost - expectedCost) > 0.01m)
            {
                problems.Add(
                    $"Role '{role}': {hours} hours x {expectedRate} = {expectedCost}, but you wrote {cost} " +
                    $"(off by {cost - expectedCost:+0.00;-0.00}).");
            }
        }

        decimal nonLabourTotal = 0;
        JsonArray nonLabour = AsArrayOrNull(draft?["nonLabourLines"]) ?? [];
        if (draft?["nonLabourLines"] is not null && AsArrayOrNull(draft["nonLabourLines"]) is null)
        {
            problems.Add("nonLabourLines must be an array of objects, not a single value. Resend it as a list.");
        }

        foreach (JsonNode? line in nonLabour)
        {
            nonLabourTotal += Decimal(line?["amount"]);
        }

        decimal componentSum = labourTotal + nonLabourTotal;

        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Your line items add up to {componentSum:0.00} {prompt.Currency} " +
            $"({labourTotal:0.00} labour across {labour.Count} line(s) + {nonLabourTotal:0.00} non-labour across {nonLabour.Count} line(s)).");

        JsonArray scenarios = AsArrayOrNull(draft?["scenarios"]) ?? [];
        if (draft?["scenarios"] is not null && AsArrayOrNull(draft["scenarios"]) is null)
        {
            problems.Add("scenarios must be an array of objects, not a single value. Resend it as a list.");
        }

        if (scenarios.Count == 0)
        {
            problems.Add("You supplied no cost scenarios. A submission without scenarios will be rejected.");
        }
        else
        {
            builder.AppendLine("Scenarios against that sum:");
            foreach (JsonNode? scenario in scenarios)
            {
                string name = scenario?["name"]?.GetValue<string>() ?? "(unnamed)";
                decimal total = Decimal(scenario?["total"]);
                decimal delta = total - componentSum;
                string verdict = componentSum == 0
                    ? "no line items to compare against"
                    : $"{delta:+0.00;-0.00} versus your line items ({delta / componentSum * 100:+0.0;-0.0}%)";
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {name}: {total:0.00} — {verdict}");
            }

            // Exactly one scenario reconciling is the expected shape, not a defect: an optimistic and
            // a pessimistic case are meant to differ from the costed plan. What must not happen is
            // none of them matching it, which means no scenario is grounded in the line items at all.
            bool anyReconciles = scenarios.Any(scenario =>
                componentSum != 0 && Math.Abs(Decimal(scenario?["total"]) - componentSum) <= componentSum * 0.005m);

            if (!anyReconciles && componentSum != 0)
            {
                problems.Add(
                    "None of your scenarios matches the sum of your own line items. At least one — the base or " +
                    "most-likely case — must equal it, or the line items describe a plan you did not cost.");
            }
        }

        if (problems.Count == 0)
        {
            builder.AppendLine("No arithmetic or rate-card problems found. You may submit.");
        }
        else
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"{problems.Count} problem(s) to fix before submitting:");
            foreach (string problem in problems)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"- {problem}");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// A JsonArray, or null when the node is absent or is some other shape. JsonNode.AsArray throws
    /// on a mismatch, and a throw here escapes the agent loop — losing the one mechanism that could
    /// have asked the model to fix its own malformed draft.
    /// </summary>
    private static JsonArray? AsArrayOrNull(JsonNode? node)
    {
        try
        {
            return node?.AsArray();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static decimal Decimal(JsonNode? node)
    {
        try
        {
            return node?.GetValue<decimal>() ?? 0m;
        }
        catch (Exception exception) when (exception is FormatException or InvalidOperationException)
        {
            return 0m;
        }
    }

    /// <summary>
    /// Takes the same three arrays as the submission, so checking a draft costs no reshaping and the
    /// model can pass exactly what it intends to submit.
    /// </summary>
    private static JsonObject CheckToolDefinition()
    {
        JsonObject submit = AnthropicCostEstimationModel.BuildToolDefinition();
        JsonNode properties = submit["input_schema"]!["properties"]!;

        return new JsonObject
        {
            ["name"] = CheckTool,
            ["description"] =
                "Reconcile a draft estimate before submitting it. Recomputes every labour line against the rate card, " +
                "totals your line items, and reports how far each scenario is from that total. Returns the problems " +
                "found, or confirms there are none.",
            ["input_schema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["labourLines"] = properties["labourLines"]!.DeepClone(),
                    ["nonLabourLines"] = properties["nonLabourLines"]!.DeepClone(),
                    ["scenarios"] = properties["scenarios"]!.DeepClone()
                },
                ["required"] = new JsonArray { "labourLines", "nonLabourLines", "scenarios" }
            }
        };
    }
}

using System.Text.Json;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions;

namespace PlanWise.Research.LlmEvaluation;

/// <summary>
/// Per-run measurements for one backlog ordering.
///
/// Split deliberately into two layers. The <c>Raw*</c> fields describe the tool call the model
/// actually emitted; the remaining fields describe what the application returned after its defensive
/// mapping. The gap between them is itself a finding: the system repairs omissions and invented keys
/// silently, so a user never sees them, and an evaluation that looked only at the returned object
/// would report perfect conformance no matter what the model did.
/// </summary>
internal sealed record PrioritisationRunMetrics(
    // --- zgodnosc: warstwa surowa (odpowiedz modelu) ---
    int InputCount,
    int RawReturnedCount,
    int RawUnknownKeys,
    int RawDuplicateKeys,
    int RawMissingKeys,
    int RawScoresOutOfRange,

    // --- zgodnosc: warstwa aplikacji (wynik po mapowaniu) ---
    int ResultCount,
    int UnrankedCount,

    // --- spojnosc ---
    double? DependencyRespectShare,
    int DependencyEdges,
    double ReasonDistinctShare,
    double ReasonMeanLength,
    double? ValueScoreRankCorrelation,

    // --- kolejnosc, do analizy powtarzalnosci ---
    IReadOnlyList<string> Order)
{
    private const string UnrankedSentinel = "Not ranked by the model";

    public static PrioritisationRunMetrics Compute(
        PrioritisationInput input,
        PrioritisationResult result,
        string rawResponseBody,
        IReadOnlyList<(string Successor, string Predecessor)> edges)
    {
        var inputKeys = input.BacklogTasks.Select(task => task.Key).ToHashSet(StringComparer.Ordinal);
        RawOrder raw = ParseRaw(rawResponseBody);

        var rawKeys = raw.Keys;
        int unknown = rawKeys.Count(key => !inputKeys.Contains(key));
        int duplicates = rawKeys.Count - rawKeys.Distinct(StringComparer.Ordinal).Count();
        int missing = inputKeys.Count(key => !rawKeys.Contains(key, StringComparer.Ordinal));

        var order = result.Ordered.Select(task => task.TaskKey).ToList();
        var positionByKey = order
            .Select((key, index) => (key, index))
            .ToDictionary(pair => pair.key, pair => pair.index, StringComparer.Ordinal);

        int respected = 0;
        int applicable = 0;
        foreach ((string successor, string predecessor) in edges)
        {
            if (positionByKey.TryGetValue(successor, out int successorAt) &&
                positionByKey.TryGetValue(predecessor, out int predecessorAt))
            {
                applicable++;
                if (predecessorAt < successorAt)
                {
                    respected++;
                }
            }
        }

        var reasons = result.Ordered.Select(task => task.Reason).ToList();
        int unranked = reasons.Count(reason => reason.StartsWith(UnrankedSentinel, StringComparison.Ordinal));

        return new PrioritisationRunMetrics(
            InputCount: input.BacklogTasks.Count,
            RawReturnedCount: rawKeys.Count,
            RawUnknownKeys: unknown,
            RawDuplicateKeys: duplicates,
            RawMissingKeys: missing,
            RawScoresOutOfRange: raw.ScoresOutOfRange,

            ResultCount: result.Ordered.Count,
            UnrankedCount: unranked,

            DependencyRespectShare: applicable == 0 ? null : respected / (double)applicable,
            DependencyEdges: applicable,
            ReasonDistinctShare: reasons.Count == 0
                ? 0d
                : reasons.Distinct(StringComparer.OrdinalIgnoreCase).Count() / (double)reasons.Count,
            ReasonMeanLength: reasons.Count == 0 ? 0d : reasons.Average(reason => reason.Length),
            ValueScoreRankCorrelation: Spearman(
                result.Ordered.Select(task => (double)task.ValueScore).ToList(),
                Enumerable.Range(0, result.Ordered.Count).Select(index => (double)-index).ToList()),

            Order: order);
    }

    private sealed record RawOrder(List<string> Keys, int ScoresOutOfRange);

    /// <summary>Reads the emitted tool call, before the application repairs it.</summary>
    private static RawOrder ParseRaw(string responseBody)
    {
        var keys = new List<string>();
        int outOfRange = 0;

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("content", out JsonElement content))
            {
                return new RawOrder(keys, outOfRange);
            }

            foreach (JsonElement block in content.EnumerateArray())
            {
                if (!block.TryGetProperty("type", out JsonElement type) || type.GetString() != "tool_use" ||
                    !block.TryGetProperty("input", out JsonElement toolInput) ||
                    !toolInput.TryGetProperty("order", out JsonElement orderElement) ||
                    orderElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement entry in orderElement.EnumerateArray())
                {
                    if (entry.TryGetProperty("taskKey", out JsonElement key) && key.ValueKind == JsonValueKind.String)
                    {
                        keys.Add(key.GetString()!);
                    }

                    foreach (string field in (string[])["valueScore", "dependencyScore", "complexityScore"])
                    {
                        if (entry.TryGetProperty(field, out JsonElement score) &&
                            score.ValueKind == JsonValueKind.Number &&
                            score.TryGetDouble(out double value) &&
                            (value < 0d || value > 1d))
                        {
                            outOfRange++;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Leave the raw layer empty; the run still reports its application-level metrics.
        }

        return new RawOrder(keys, outOfRange);
    }

    /// <summary>Mean absolute rank displacement between two orderings of the same keys (D_rank).</summary>
    public static double? MeanRankDisplacement(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        var secondPositions = second
            .Select((key, index) => (key, index))
            .ToDictionary(pair => pair.key, pair => pair.index, StringComparer.Ordinal);

        var displacements = new List<double>();
        for (int i = 0; i < first.Count; i++)
        {
            if (secondPositions.TryGetValue(first[i], out int other))
            {
                displacements.Add(Math.Abs(i - other));
            }
        }

        return displacements.Count == 0 ? null : displacements.Average();
    }

    public static double? TopKJaccard(IReadOnlyList<string> first, IReadOnlyList<string> second, int k)
    {
        if (first.Count < k || second.Count < k)
        {
            return null;
        }

        var a = first.Take(k).ToHashSet(StringComparer.Ordinal);
        var b = second.Take(k).ToHashSet(StringComparer.Ordinal);
        int intersection = a.Count(key => b.Contains(key));
        int union = a.Count + b.Count - intersection;
        return union == 0 ? null : intersection / (double)union;
    }

    public static double? SpearmanBetween(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        var secondPositions = second
            .Select((key, index) => (key, index))
            .ToDictionary(pair => pair.key, pair => pair.index, StringComparer.Ordinal);

        var x = new List<double>();
        var y = new List<double>();
        for (int i = 0; i < first.Count; i++)
        {
            if (secondPositions.TryGetValue(first[i], out int other))
            {
                x.Add(i);
                y.Add(other);
            }
        }

        return Spearman(x, y);
    }

    /// <summary>Pearson correlation over values that are already ranks, i.e. Spearman's rho.</summary>
    private static double? Spearman(IReadOnlyList<double> x, IReadOnlyList<double> y)
    {
        if (x.Count < 3 || x.Count != y.Count)
        {
            return null;
        }

        double meanX = x.Average();
        double meanY = y.Average();
        double covariance = 0d;
        double varianceX = 0d;
        double varianceY = 0d;

        for (int i = 0; i < x.Count; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;
            covariance += dx * dy;
            varianceX += dx * dx;
            varianceY += dy * dy;
        }

        double denominator = Math.Sqrt(varianceX * varianceY);
        return denominator == 0d ? null : covariance / denominator;
    }
}

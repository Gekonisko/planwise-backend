using PlanWise.Modules.CostEstimation.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Estimates;

namespace PlanWise.Research.LlmEvaluation;

/// <summary>
/// Per-run measurements for one cost estimate, grouped by the three axes of the research question.
/// Every field is computed from the model's own answer against the input it was given — no external
/// ground truth about real spend is used or needed.
/// </summary>
internal sealed record CostRunMetrics(
    // --- zgodnosc z danymi projektu ---
    int LabourLineCount,
    int RolesNotInRateCard,
    int RolesCaseMismatchOnly,
    double RateExactShare,
    double RateCardCoverage,
    decimal MaxRateDeviation,

    // --- spojnosc ---
    int ScenarioCount,
    bool ScenarioRolesRecognisable,
    bool ScenarioNamesLiteral,
    bool ScenarioTotalsMonotonic,
    bool PercentilesMonotonic,
    double LineArithmeticShare,
    decimal MaxLineArithmeticError,
    decimal RealisticTotal,
    decimal ComponentsSum,
    double? SumRelativeDeviation,
    double? PriorityBreakdownRelativeDeviation,
    bool AllNonNegative,
    bool ReductionsWithinTotal,

    // --- opis wyniku ---
    decimal TotalHours,
    int AssumptionCount,
    int ReductionCount,
    int NonLabourCount,
    IReadOnlyDictionary<string, decimal> HoursByRole)
{
    private const decimal Tolerance = 0.01m;

    public static CostRunMetrics Compute(CostEstimationPrompt prompt, CostEstimateResult result)
    {
        var rateByRole = prompt.RateCard.ToDictionary(rate => rate.Role, rate => rate.HourlyRate, StringComparer.Ordinal);
        var rateByRoleLoose = prompt.RateCard
            .GroupBy(rate => rate.Role, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().HourlyRate, StringComparer.OrdinalIgnoreCase);

        // Every collection is defended against null rather than trusted. The contract declares them
        // non-nullable, but they are produced by deserialising a model-authored tool call: an omitted
        // array arrives as a null reference, not an empty list. That this is reachable at all is a
        // finding in its own right — the production handler does not guard here either.
        var lines = result.LabourLines ?? [];
        var scenarios = result.Scenarios ?? [];
        var nonLabour = result.NonLabourLines ?? [];
        var priorityBreakdown = result.PriorityBreakdown ?? [];
        var assumptions = result.Assumptions ?? [];
        var reductions = result.Reductions ?? [];

        int notInCard = lines.Count(line => !rateByRoleLoose.ContainsKey(line.Role));
        int caseOnly = lines.Count(line => !rateByRole.ContainsKey(line.Role) && rateByRoleLoose.ContainsKey(line.Role));

        var matched = lines.Where(line => rateByRoleLoose.ContainsKey(line.Role)).ToList();
        int exactRate = matched.Count(line => Math.Abs(line.HourlyRate - rateByRoleLoose[line.Role]) <= Tolerance);
        decimal maxRateDeviation = matched.Count == 0
            ? 0m
            : matched.Max(line => Math.Abs(line.HourlyRate - rateByRoleLoose[line.Role]));

        var usedRoles = lines.Select(line => line.Role).ToHashSet(StringComparer.OrdinalIgnoreCase);
        double coverage = prompt.RateCard.Count == 0
            ? 0d
            : prompt.RateCard.Count(rate => usedRoles.Contains(rate.Role)) / (double)prompt.RateCard.Count;

        int arithmeticOk = lines.Count(line => Math.Abs(line.Cost - line.Hours * line.HourlyRate) <= Tolerance);
        decimal maxArithmeticError = lines.Count == 0
            ? 0m
            : lines.Max(line => Math.Abs(line.Cost - line.Hours * line.HourlyRate));

        // The schema never constrains the wording, and in practice the middle scenario is labelled
        // "Most likely" or "base case" as often as "realistic" — so synonyms are accepted before
        // falling back to position. Matching on the literal word alone would misreport the headline
        // figure as missing when it is merely named differently.
        CostScenario? realistic = PickAny(scenarios, MiddleMarkers)
            ?? (scenarios.Count == 3 ? scenarios[1] : scenarios.FirstOrDefault());

        decimal componentsSum = lines.Sum(line => line.Cost) + nonLabour.Sum(line => line.Amount);
        decimal realisticTotal = realistic?.Total ?? 0m;
        double? sumDeviation = realisticTotal > 0m
            ? (double)Math.Abs(realisticTotal - componentsSum) / (double)realisticTotal
            : null;

        decimal priorityTotal = priorityBreakdown.Sum(line => line.Total);
        double? priorityDeviation = realisticTotal > 0m && priorityBreakdown.Count > 0
            ? (double)Math.Abs(realisticTotal - priorityTotal) / (double)realisticTotal
            : null;

        return new CostRunMetrics(
            LabourLineCount: lines.Count,
            RolesNotInRateCard: notInCard,
            RolesCaseMismatchOnly: caseOnly,
            RateExactShare: matched.Count == 0 ? 0d : exactRate / (double)matched.Count,
            RateCardCoverage: coverage,
            MaxRateDeviation: maxRateDeviation,

            ScenarioCount: scenarios.Count,
            ScenarioRolesRecognisable: HasRecognisableRoles(scenarios),
            ScenarioNamesLiteral: HasLiteralNames(scenarios),
            ScenarioTotalsMonotonic: IsMonotonic(scenarios.Select(scenario => scenario.Total)),
            PercentilesMonotonic: IsMonotonic(scenarios.Select(scenario => (decimal)scenario.Percentile)),
            LineArithmeticShare: lines.Count == 0 ? 0d : arithmeticOk / (double)lines.Count,
            MaxLineArithmeticError: maxArithmeticError,
            RealisticTotal: realisticTotal,
            ComponentsSum: componentsSum,
            SumRelativeDeviation: sumDeviation,
            PriorityBreakdownRelativeDeviation: priorityDeviation,
            AllNonNegative:
                lines.All(line => line.Hours >= 0 && line.HourlyRate >= 0 && line.Cost >= 0) &&
                nonLabour.All(line => line.Amount >= 0) &&
                scenarios.All(scenario => scenario.Total >= 0),
            ReductionsWithinTotal: realisticTotal <= 0m || reductions.All(reduction => reduction.Saving <= realisticTotal),

            TotalHours: lines.Sum(line => line.Hours),
            AssumptionCount: assumptions.Count,
            ReductionCount: reductions.Count,
            NonLabourCount: nonLabour.Count,
            HoursByRole: lines
                .GroupBy(line => line.Role, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(line => line.Hours), StringComparer.OrdinalIgnoreCase));
    }

    private static readonly string[] OptimisticMarkers = ["optimistic", "best case"];
    private static readonly string[] MiddleMarkers = ["realistic", "most likely", "base case", "baseline", "expected", "likely"];
    private static readonly string[] PessimisticMarkers = ["pessimistic", "worst case", "risk-adjusted"];

    private static CostScenario? PickScenario(IReadOnlyList<CostScenario> scenarios, string needle) =>
        scenarios.FirstOrDefault(scenario =>
            (scenario.Name ?? string.Empty).Contains(needle, StringComparison.OrdinalIgnoreCase));

    private static CostScenario? PickAny(IReadOnlyList<CostScenario> scenarios, string[] needles) =>
        needles.Select(needle => PickScenario(scenarios, needle)).FirstOrDefault(found => found is not null);

    /// <summary>Three scenarios whose names are semantically identifiable, whatever wording is used.</summary>
    private static bool HasRecognisableRoles(IReadOnlyList<CostScenario> scenarios) =>
        scenarios.Count == 3 &&
        PickAny(scenarios, OptimisticMarkers) is not null &&
        PickAny(scenarios, MiddleMarkers) is not null &&
        PickAny(scenarios, PessimisticMarkers) is not null;

    /// <summary>The stricter reading of WF-17: the three variants named literally.</summary>
    private static bool HasLiteralNames(IReadOnlyList<CostScenario> scenarios) =>
        scenarios.Count == 3 &&
        PickScenario(scenarios, "optimistic") is not null &&
        PickScenario(scenarios, "realistic") is not null &&
        PickScenario(scenarios, "pessimistic") is not null;

    private static bool IsMonotonic(IEnumerable<decimal> values)
    {
        decimal? previous = null;
        foreach (decimal value in values)
        {
            if (previous is decimal last && value < last)
            {
                return false;
            }

            previous = value;
        }

        return true;
    }
}

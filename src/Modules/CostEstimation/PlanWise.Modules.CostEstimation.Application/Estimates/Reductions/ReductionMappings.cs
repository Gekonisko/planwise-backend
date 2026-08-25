using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.Reductions;

internal static class ReductionMappings
{
    // BaselineTotal is the run's own likely-case (percentile-50) scenario, same reference point burn
    // uses — ProjectedTotal subtracts every currently-applied recommendation's saving from it. Shared
    // by all three reduction endpoints so "recomputes the projected total" means the same computation
    // everywhere, not three slightly different ones.
    public static ReductionsResponse BuildResponse(CostEstimateRun run, IReadOnlyList<AppliedReduction> applied)
    {
        CostEstimateResult result = CostEstimateMappings.DeserializeResult(run.ResultJson);
        decimal baselineTotal = CostEstimateMappings.PickScenarioTotal(result.Scenarios, 50);

        var appliedIds = new HashSet<Guid>(applied.Select(a => a.ReductionId));
        decimal projectedTotal = baselineTotal - result.Reductions
            .Where(reduction => appliedIds.Contains(reduction.Id))
            .Sum(reduction => reduction.Saving);

        IReadOnlyList<ReductionResponse> reductions = result.Reductions
            .Select(reduction => new ReductionResponse(
                reduction.Id, reduction.Description, reduction.Saving, reduction.Effect, reduction.Confidence,
                appliedIds.Contains(reduction.Id)))
            .ToList();

        return new ReductionsResponse(run.Id, run.Currency, baselineTotal, projectedTotal, reductions);
    }
}

using PlanWise.Common.Domain;

namespace PlanWise.Modules.CostEstimation.Domain.Estimates;

// Tracks which reduction recommendations a user has accepted, separately from the immutable
// ResultJson blob those recommendations live in (CostEstimateRun.ResultJson is never mutated after
// creation — see its own comment). ReductionId matches a CostReduction.Id inside that JSON, not a
// row in this table's own identity space.
public sealed class AppliedReduction : Entity
{
    private AppliedReduction()
    {
    }

    private AppliedReduction(Guid runId, Guid reductionId, DateTime appliedAtUtc)
    {
        Id = Guid.NewGuid();
        RunId = runId;
        ReductionId = reductionId;
        AppliedAtUtc = appliedAtUtc;
    }

    public Guid RunId { get; private set; }
    public Guid ReductionId { get; private set; }
    public DateTime AppliedAtUtc { get; private set; }

    public static AppliedReduction Create(Guid runId, Guid reductionId, DateTime appliedAtUtc) =>
        new(runId, reductionId, appliedAtUtc);
}

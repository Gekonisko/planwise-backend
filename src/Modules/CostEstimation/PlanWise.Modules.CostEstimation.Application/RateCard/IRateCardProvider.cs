using PlanWise.Modules.CostEstimation.Application.Abstractions;

namespace PlanWise.Modules.CostEstimation.Application.RateCard;

// No PUT endpoint exists in the spec for rates ("also editable in settings" is only mentioned for
// availability, section 9) — this is a fixed reference table, not project- or org-configurable yet.
public interface IRateCardProvider
{
    IReadOnlyList<RoleRate> GetRates();
}

public sealed class DefaultRateCardProvider : IRateCardProvider
{
    private static readonly RoleRate[] Rates =
    [
        new RoleRate("Developer", 75m, "USD"),
        new RoleRate("Lead Developer", 110m, "USD"),
        new RoleRate("Designer", 65m, "USD"),
        new RoleRate("QA Engineer", 60m, "USD"),
        new RoleRate("Project Manager", 90m, "USD")
    ];

    public IReadOnlyList<RoleRate> GetRates() => Rates;
}

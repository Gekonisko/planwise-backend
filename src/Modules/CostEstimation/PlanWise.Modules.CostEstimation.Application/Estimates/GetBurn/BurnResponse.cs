namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetBurn;

public sealed record BurnPoint(DateOnly Date, decimal ActualSpend);

public sealed record BurnForecast(decimal P50Total, decimal P90Total);

public sealed record BurnResponse(
    Guid CostEstimateId,
    decimal? Budget,
    string Currency,
    IReadOnlyList<BurnPoint> ActualSpendSeries,
    BurnForecast Forecast,
    DateTime AsOfUtc);

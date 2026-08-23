using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.CostEstimation.Presentation;

public sealed class CostEstimationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        BudgetEndpoints.MapEndpoints(app);
        RateCardEndpoints.MapEndpoints(app);
        CostEstimateEndpoints.MapEndpoints(app);
    }
}

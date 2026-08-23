using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.RiskPrediction.Presentation;

public sealed class RiskPredictionEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ForecastEndpoints.MapEndpoints(app);
        RiskEndpoints.MapEndpoints(app);
    }
}

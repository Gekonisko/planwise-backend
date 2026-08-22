using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.Delivery.Presentation;

public sealed class DeliveryEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        SprintEndpoints.MapEndpoints(app);
        TaskEndpoints.MapEndpoints(app);
    }
}

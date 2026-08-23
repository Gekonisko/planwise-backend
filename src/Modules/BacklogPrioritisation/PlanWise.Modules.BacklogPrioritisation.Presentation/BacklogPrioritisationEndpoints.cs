using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.BacklogPrioritisation.Presentation;

public sealed class BacklogPrioritisationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        PriorityEndpoints.MapEndpoints(app);
    }
}

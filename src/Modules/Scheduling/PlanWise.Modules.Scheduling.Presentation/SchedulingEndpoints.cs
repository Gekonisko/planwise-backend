using Microsoft.AspNetCore.Routing;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.Scheduling.Presentation;

public sealed class SchedulingEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        ScheduleEndpoints.MapEndpoints(app);
        MilestoneEndpoints.MapEndpoints(app);
        OptimisationEndpoints.MapEndpoints(app);
    }
}

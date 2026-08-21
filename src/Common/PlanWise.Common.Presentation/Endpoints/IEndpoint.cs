using Microsoft.AspNetCore.Routing;

namespace PlanWise.Common.Presentation.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}

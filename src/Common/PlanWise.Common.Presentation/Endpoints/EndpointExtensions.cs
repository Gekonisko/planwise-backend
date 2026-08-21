using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PlanWise.Common.Presentation.Endpoints;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, params Type[] endpointTypes)
    {
        services.TryAddEnumerable(endpointTypes.Select(type =>
            ServiceDescriptor.Transient(typeof(IEndpoint), type)));
        return services;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        foreach (IEndpoint endpoint in app.Services.GetRequiredService<IEnumerable<IEndpoint>>())
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}

using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace PlanWise.Common.Application;

public static class ApplicationConfiguration
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        Assembly[] moduleAssemblies,
        params Type[] pipelineBehaviors)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssemblies(moduleAssemblies);

            foreach (Type pipelineBehavior in pipelineBehaviors)
            {
                config.AddOpenBehavior(pipelineBehavior);
            }
        });

        services.AddValidatorsFromAssemblies(moduleAssemblies, includeInternalTypes: true);
        return services;
    }
}
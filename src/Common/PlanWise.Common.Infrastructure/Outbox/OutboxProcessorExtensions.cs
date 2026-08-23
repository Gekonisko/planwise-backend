using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PlanWise.Common.Infrastructure.Outbox;

public static class OutboxProcessorExtensions
{
    public static IServiceCollection AddOutboxProcessor<TDbContext>(
        this IServiceCollection services,
        Assembly domainEventHandlersAssembly,
        OutboxProcessorOptions? options = null)
        where TDbContext : DbContext
    {
        services.AddHostedService(provider => new OutboxProcessor<TDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            domainEventHandlersAssembly,
            options ?? new OutboxProcessorOptions(),
            provider.GetRequiredService<ILogger<OutboxProcessor<TDbContext>>>()));

        return services;
    }
}

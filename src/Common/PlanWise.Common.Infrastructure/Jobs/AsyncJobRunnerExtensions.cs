using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PlanWise.Common.Infrastructure.Jobs;

public static class AsyncJobRunnerExtensions
{
    public static IServiceCollection AddAsyncJobRunner(
        this IServiceCollection services,
        AsyncJobRunnerOptions? options = null)
    {
        services.AddHostedService(provider => new AsyncJobRunner(
            provider.GetRequiredService<IServiceScopeFactory>(),
            options ?? new AsyncJobRunnerOptions(),
            provider.GetRequiredService<ILogger<AsyncJobRunner>>()));

        return services;
    }
}

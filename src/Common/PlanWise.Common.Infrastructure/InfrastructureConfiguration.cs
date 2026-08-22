using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Data;
using PlanWise.Common.Infrastructure.Clock;
using PlanWise.Common.Infrastructure.Data;

namespace PlanWise.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    public static IServiceCollection AddCommonInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Database connection string is not configured");

        services.TryAddSingleton(new NpgsqlDataSourceBuilder(connectionString).Build());
        services.TryAddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.TryAddSingleton<IDateTimeProvider, DateTimeProvider>();
        return services;
    }
}
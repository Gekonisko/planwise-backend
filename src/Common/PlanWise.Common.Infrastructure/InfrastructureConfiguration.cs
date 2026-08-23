using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Data;
using PlanWise.Common.Infrastructure.Authentication;
using PlanWise.Common.Infrastructure.Clock;
using PlanWise.Common.Infrastructure.Data;
using PlanWise.Common.Infrastructure.Database;
using PlanWise.Common.Infrastructure.Jobs;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Common.Presentation.Jobs;

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

        services.AddEndpoints(typeof(JobEndpoints).Assembly);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddDbContext<CommonDbContext>((_, options) => options
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    Schemas.Common))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IAsyncJobService, AsyncJobService>();
        services.AddAsyncJobRunner();

        return services;
    }

    public static void ApplyCommonMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CommonDbContext>().Database.Migrate();
    }
}

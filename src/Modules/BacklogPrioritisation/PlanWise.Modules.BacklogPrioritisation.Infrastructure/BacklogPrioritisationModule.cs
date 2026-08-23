using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Authentication;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Data;
using PlanWise.Modules.BacklogPrioritisation.Application.Priorities;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;
using PlanWise.Modules.BacklogPrioritisation.Infrastructure.Authentication;
using PlanWise.Modules.BacklogPrioritisation.Infrastructure.Database;
using PlanWise.Modules.BacklogPrioritisation.Infrastructure.Priorities;
using PlanWise.Modules.BacklogPrioritisation.Presentation;

namespace PlanWise.Modules.BacklogPrioritisation.Infrastructure;

public static class BacklogPrioritisationModule
{
    public static IServiceCollection AddBacklogPrioritisationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(typeof(BacklogPrioritisationEndpoints).Assembly);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddDbContext<BacklogPrioritisationDbContext>((_, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    Schemas.BacklogPrioritisation))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IPriorityRunRepository, PriorityRunRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<BacklogPrioritisationDbContext>());

        services.AddScoped<IAsyncJobHandler, PriorityScoringJobHandler>();

        return services;
    }

    public static void ApplyBacklogPrioritisationMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<BacklogPrioritisationDbContext>().Database.Migrate();
    }
}

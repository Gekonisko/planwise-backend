using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Application.Optimisation;
using PlanWise.Modules.Scheduling.Domain.Milestones;
using PlanWise.Modules.Scheduling.Domain.Optimisation;
using PlanWise.Modules.Scheduling.Domain.Schedule;
using PlanWise.Modules.Scheduling.Infrastructure.Authentication;
using PlanWise.Modules.Scheduling.Infrastructure.Database;
using PlanWise.Modules.Scheduling.Infrastructure.Milestones;
using PlanWise.Modules.Scheduling.Infrastructure.Optimisation;
using PlanWise.Modules.Scheduling.Infrastructure.Schedule;
using PlanWise.Modules.Scheduling.Presentation;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.Scheduling.Infrastructure;

public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(typeof(SchedulingEndpoints).Assembly);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddDbContext<SchedulingDbContext>((_, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    Schemas.Scheduling))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IMilestoneRepository, MilestoneRepository>();
        services.AddScoped<IScheduleItemRepository, ScheduleItemRepository>();
        services.AddScoped<IScheduleProposalRepository, ScheduleProposalRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SchedulingDbContext>());
        services.AddScoped<IAsyncJobHandler, ScheduleOptimisationJobHandler>();

        return services;
    }

    public static void ApplySchedulingMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SchedulingDbContext>().Database.Migrate();
    }
}

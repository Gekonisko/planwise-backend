using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Infrastructure.Outbox;
using PlanWise.Modules.Delivery.Application;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Application.Activity.EventHandlers;
using PlanWise.Modules.Delivery.Domain.Activity;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;
using PlanWise.Modules.Delivery.Infrastructure.Activity;
using PlanWise.Modules.Delivery.Infrastructure.Authentication;
using PlanWise.Modules.Delivery.Infrastructure.Database;
using PlanWise.Modules.Delivery.Infrastructure.Sprints;
using PlanWise.Modules.Delivery.Infrastructure.Tasks;
using PlanWise.Modules.Delivery.Presentation;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.Delivery.Infrastructure;

public static class DeliveryModule
{
    public static IServiceCollection AddDeliveryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(typeof(DeliveryEndpoints).Assembly);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddDbContext<DeliveryDbContext>((_, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    Schemas.Delivery))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new InsertOutboxMessagesInterceptor()));
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<IProjectTaskRepository, ProjectTaskRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.AddScoped<IProjectTasksService, ProjectTasksService>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DeliveryDbContext>());

        services.AddScoped<SprintStartedActivityHandler>();
        services.AddScoped<SprintCompletedActivityHandler>();
        services.AddScoped<ProjectTaskCreatedActivityHandler>();
        services.AddScoped<ProjectTaskMovedActivityHandler>();
        services.AddScoped<ProjectTaskCommentAddedActivityHandler>();
        services.AddOutboxProcessor<DeliveryDbContext>(AssemblyReference.Assembly);

        return services;
    }

    public static void ApplyDeliveryMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<DeliveryDbContext>().Database.Migrate();
    }
}

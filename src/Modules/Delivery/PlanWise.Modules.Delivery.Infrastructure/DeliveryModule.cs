using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanWise.Modules.Delivery.Application.Abstractions.Authentication;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;
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
            .UseSnakeCaseNamingConvention());
        services.AddScoped<ISprintRepository, SprintRepository>();
        services.AddScoped<IProjectTaskRepository, ProjectTaskRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<DeliveryDbContext>());

        return services;
    }

    public static void ApplyDeliveryMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<DeliveryDbContext>().Database.Migrate();
    }
}

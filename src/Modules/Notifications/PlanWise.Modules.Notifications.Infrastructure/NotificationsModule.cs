using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Modules.Notifications.Application.Abstractions.Authentication;
using PlanWise.Modules.Notifications.Application.Abstractions.Data;
using PlanWise.Modules.Notifications.Domain.Notifications;
using PlanWise.Modules.Notifications.Infrastructure.Authentication;
using PlanWise.Modules.Notifications.Infrastructure.Database;
using PlanWise.Modules.Notifications.Infrastructure.Notifications;
using PlanWise.Modules.Notifications.Presentation;

namespace PlanWise.Modules.Notifications.Infrastructure;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(typeof(NotificationsEndpoints).Assembly);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddDbContext<NotificationsDbContext>((_, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    Schemas.Notifications))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<INotificationPublisher, NotificationPublisherService>();

        return services;
    }

    public static void ApplyNotificationsMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.Migrate();
    }
}

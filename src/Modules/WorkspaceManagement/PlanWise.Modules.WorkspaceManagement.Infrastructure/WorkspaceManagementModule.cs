using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;
using PlanWise.Modules.WorkspaceManagement.Infrastructure.Authentication;
using PlanWise.Modules.WorkspaceManagement.Infrastructure.Database;
using PlanWise.Modules.WorkspaceManagement.Infrastructure.Projects;
using PlanWise.Modules.WorkspaceManagement.Presentation;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.WorkspaceManagement.Infrastructure;

public static class WorkspaceManagementModule
{
    public static IServiceCollection AddWorkspaceManagementModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(typeof(WorkspaceManagementEndpoints).Assembly);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddDbContext<WorkspaceManagementDbContext>((_, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    Schemas.WorkspaceManagement))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectAccessService, ProjectAccessService>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<WorkspaceManagementDbContext>());

        return services;
    }

    public static void MapWorkspaceManagementEndpoints(this IEndpointRouteBuilder app) =>
        WorkspaceEndpoints.MapEndpoints(app);

    public static void ApplyMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<WorkspaceManagementDbContext>().Database.Migrate();
    }
}
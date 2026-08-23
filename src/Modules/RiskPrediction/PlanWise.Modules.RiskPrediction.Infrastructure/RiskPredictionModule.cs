using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Data;
using PlanWise.Modules.RiskPrediction.Application.Risks;
using PlanWise.Modules.RiskPrediction.Domain.Risks;
using PlanWise.Modules.RiskPrediction.Infrastructure.Authentication;
using PlanWise.Modules.RiskPrediction.Infrastructure.Database;
using PlanWise.Modules.RiskPrediction.Infrastructure.Risks;
using PlanWise.Modules.RiskPrediction.Presentation;

namespace PlanWise.Modules.RiskPrediction.Infrastructure;

public static class RiskPredictionModule
{
    public static IServiceCollection AddRiskPredictionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(typeof(RiskPredictionEndpoints).Assembly);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddDbContext<RiskPredictionDbContext>((_, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    Schemas.RiskPrediction))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IRiskAssessmentRunRepository, RiskAssessmentRunRepository>();
        services.AddScoped<ITaskRiskAssessmentRepository, TaskRiskAssessmentRepository>();
        services.AddScoped<ISprintForecastRepository, SprintForecastRepository>();
        services.AddScoped<IRiskInsightsService, RiskInsightsService>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<RiskPredictionDbContext>());

        services.AddScoped<IAsyncJobHandler, RiskAssessmentJobHandler>();

        return services;
    }

    public static void ApplyRiskPredictionMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RiskPredictionDbContext>().Database.Migrate();
    }
}

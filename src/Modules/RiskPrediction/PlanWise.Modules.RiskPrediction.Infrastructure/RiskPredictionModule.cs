using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Modules.RiskPrediction.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Authentication;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Data;
using PlanWise.Modules.RiskPrediction.Application.Risks;
using PlanWise.Modules.RiskPrediction.Application.Training;
using PlanWise.Modules.RiskPrediction.Domain.Risks;
using PlanWise.Modules.RiskPrediction.Domain.Training;
using PlanWise.Modules.RiskPrediction.Infrastructure.Authentication;
using PlanWise.Modules.RiskPrediction.Infrastructure.Database;
using PlanWise.Modules.RiskPrediction.Infrastructure.Risks;
using PlanWise.Modules.RiskPrediction.Infrastructure.Training;
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
        services.AddScoped<ITaskFeatureSnapshotRepository, TaskFeatureSnapshotRepository>();
        services.AddScoped<RiskTrainingDataRecorder>();
        services.AddScoped<IRiskInsightsService, RiskInsightsService>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<RiskPredictionDbContext>());

        // The registered model decides how risk is predicted; everything upstream of here is
        // model-agnostic.
        //
        // The scorecard remains the default deliberately. The trained model scores about 0.59 ROC
        // AUC on unseen projects, which is real signal but no landslide over a tuned heuristic, and
        // its probabilities were calibrated on other organisations' data. Making it opt-in means the
        // switch can be justified by this system's own captured outcomes — which is exactly what
        // task_feature_snapshots is accumulating — rather than by the study alone.
        bool useTrainedModel = configuration.GetValue<bool>("RiskPrediction:UseTrainedModel");
        if (useTrainedModel)
        {
            services.AddScoped<IRiskPredictionModel, TrainedSlipRiskModel>();
        }
        else
        {
            services.AddScoped<IRiskPredictionModel, WeightedScorecardRiskModel>();
        }

        services.AddScoped<IAsyncJobHandler, RiskAssessmentJobHandler>();

        return services;
    }

    public static void ApplyRiskPredictionMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RiskPredictionDbContext>().Database.Migrate();
    }
}

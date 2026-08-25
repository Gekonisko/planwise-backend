using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application;
using PlanWise.Modules.CostEstimation.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Data;
using PlanWise.Modules.CostEstimation.Application.Estimates;
using PlanWise.Modules.CostEstimation.Application.RateCard;
using PlanWise.Modules.CostEstimation.Domain.Budget;
using PlanWise.Modules.CostEstimation.Domain.Estimates;
using PlanWise.Modules.CostEstimation.Infrastructure.Authentication;
using PlanWise.Modules.CostEstimation.Infrastructure.Budget;
using PlanWise.Modules.CostEstimation.Infrastructure.Database;
using PlanWise.Modules.CostEstimation.Infrastructure.Estimates;
using PlanWise.Modules.CostEstimation.Infrastructure.Llm;
using PlanWise.Modules.CostEstimation.Presentation;
using PlanWise.Common.Presentation.Endpoints;

namespace PlanWise.Modules.CostEstimation.Infrastructure;

public static class CostEstimationModule
{
    public static IServiceCollection AddCostEstimationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddEndpoints(typeof(CostEstimationEndpoints).Assembly);
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddDbContext<CostEstimationDbContext>((_, options) => options
            .UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName,
                    Schemas.CostEstimation))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IProjectBudgetRepository, ProjectBudgetRepository>();
        services.AddScoped<ICostEstimateRunRepository, CostEstimateRunRepository>();
        services.AddScoped<IAppliedReductionRepository, AppliedReductionRepository>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CostEstimationDbContext>());
        services.AddSingleton<IRateCardProvider, DefaultRateCardProvider>();

        services.AddOptions<AnthropicOptions>().BindConfiguration(AnthropicOptions.SectionName);
        services.AddHttpClient<ICostEstimationModel, AnthropicCostEstimationModel>((provider, client) =>
        {
            AnthropicOptions options = provider.GetRequiredService<IOptions<AnthropicOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        services.AddScoped<IAsyncJobHandler, CostEstimationJobHandler>();

        return services;
    }

    public static void ApplyCostEstimationMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CostEstimationDbContext>().Database.Migrate();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Authentication;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Data;
using PlanWise.Modules.BacklogPrioritisation.Application.Priorities;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;
using PlanWise.Modules.BacklogPrioritisation.Infrastructure.Authentication;
using PlanWise.Modules.BacklogPrioritisation.Infrastructure.Database;
using PlanWise.Modules.BacklogPrioritisation.Infrastructure.Llm;
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
        // The registered model decides how the backlog is ordered; everything upstream of here is
        // model-agnostic. Which one is registered depends on whether an API key is configured: the
        // LLM is the better ordering, but a missing key must degrade to the scorecard rather than
        // fail every run — a developer without a key still gets a working priorities screen.
        services.AddOptions<AnthropicOptions>().BindConfiguration(AnthropicOptions.SectionName);
        if (string.IsNullOrWhiteSpace(configuration[$"{AnthropicOptions.SectionName}:ApiKey"]))
        {
            services.AddScoped<IBacklogPrioritisationModel, WeightedScorecardPrioritisationModel>();
        }
        else
        {
            services.AddHttpClient<IBacklogPrioritisationModel, AnthropicBacklogPrioritisationModel>((provider, client) =>
            {
                AnthropicOptions anthropic = provider.GetRequiredService<IOptions<AnthropicOptions>>().Value;
                client.BaseAddress = new Uri(anthropic.BaseUrl);
                // A backlog ordering is a single reasoning-heavy call; the default 100s HttpClient
                // timeout is not enough headroom for a large backlog.
                client.Timeout = TimeSpan.FromMinutes(5);
            });
        }


        services.AddScoped<IAsyncJobHandler, PriorityScoringJobHandler>();

        return services;
    }

    public static void ApplyBacklogPrioritisationMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<BacklogPrioritisationDbContext>().Database.Migrate();
    }
}

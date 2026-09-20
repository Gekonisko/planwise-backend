using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Optimisation;
using PlanWise.Modules.Scheduling.Domain.Milestones;
using PlanWise.Modules.Scheduling.Domain.Optimisation;
using PlanWise.Modules.Scheduling.Domain.Schedule;
using PlanWise.Modules.Scheduling.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using PlanWise.Modules.Scheduling.Infrastructure.Database;
using PlanWise.Modules.Scheduling.Infrastructure.Llm;
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
        // The registered optimiser decides how assignments are chosen; the job handler, the proposal
        // aggregate and the apply endpoints are all indifferent to it.
        //
        // CP-SAT is the default here, unlike RiskPrediction's trained model. The difference is that a
        // solver optimises the stated objective directly rather than estimating something — it cannot
        // be worse than the greedy baseline on makespan, and when it cannot solve at all it says so
        // and hands back the greedy result.
        //
        // Scheduling:Optimiser picks between the three: "cpsat" (default), "greedy", or "agent" for
        // the LLM agent; the older Scheduling:UseConstraintSolver=false still pins greedy. The agent
        // is opt-in and stays that way: it costs a billed request per turn and, unlike the solver,
        // guarantees nothing about the finish date. It is registered so the three can be compared on
        // identical input, not because it is the better default.
        string optimiser = configuration.GetValue<string>("Scheduling:Optimiser")
            ?? (configuration.GetValue("Scheduling:UseConstraintSolver", defaultValue: true) ? "cpsat" : "greedy");

        switch (optimiser.ToLowerInvariant())
        {
            case "agent":
                services.AddOptions<AnthropicSchedulingOptions>().BindConfiguration(AnthropicSchedulingOptions.SectionName);
                services.AddHttpClient<IScheduleOptimisationModel, AgenticScheduleOptimiser>((provider, client) =>
                {
                    AnthropicSchedulingOptions anthropic = provider.GetRequiredService<IOptions<AnthropicSchedulingOptions>>().Value;
                    client.BaseAddress = new Uri(anthropic.BaseUrl);
                    // An agent turn is a full model call and there may be several of them, so the
                    // per-request timeout has to allow for the slowest single turn, not the average.
                    client.Timeout = TimeSpan.FromMinutes(5);
                });
                break;

            case "greedy":
                services.AddScoped<IScheduleOptimisationModel, GreedyCapacityBalancer>();
                break;

            default:
                services.AddScoped<IScheduleOptimisationModel, CpSatScheduleOptimiser>();
                break;
        }

        services.AddScoped<IAsyncJobHandler, ScheduleOptimisationJobHandler>();

        return services;
    }

    public static void ApplySchedulingMigrations(this IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SchedulingDbContext>().Database.Migrate();
    }
}

using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Authentication;
using PlanWise.Modules.CostEstimation.Domain;
using PlanWise.Modules.CostEstimation.Domain.Budget;
using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Estimates.GetBurn;

// "Actual spend" has no real time-tracking data behind it anywhere in this system (same gap stated
// on the estimate itself) — this is a proxy: cumulative completed points as a fraction of total
// backlog points, applied to the run's own likely-case (p50) scenario total. p50/p90 forecast totals
// are real, though — they're the model's own scenario totals for this run, picked by nearest
// percentile, not fabricated for this endpoint.
internal sealed class GetCostEstimateBurnQueryHandler(
    ICostEstimateRunRepository runRepository,
    IProjectBudgetRepository budgetRepository,
    IProjectTasksService projectTasksService,
    IProjectAccessService projectAccessService,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetCostEstimateBurnQuery, BurnResponse>
{
    public async Task<Result<BurnResponse>> Handle(GetCostEstimateBurnQuery request, CancellationToken cancellationToken)
    {
        CostEstimateRun? run = await runRepository.GetAsync(request.RunId, cancellationToken);
        if (run is null)
        {
            return Result.Failure<BurnResponse>(CostEstimateErrors.RunNotFound(request.RunId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(run.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<BurnResponse>(CostEstimateErrors.RunNotFound(request.RunId));
        }

        CostEstimateResult result = CostEstimateMappings.DeserializeResult(run.ResultJson);
        decimal p50Total = CostEstimateMappings.PickScenarioTotal(result.Scenarios, 50);
        decimal p90Total = CostEstimateMappings.PickScenarioTotal(result.Scenarios, 90);

        IReadOnlyList<CostEstimationTaskSummary> tasks = await projectTasksService.GetCostEstimationTasksAsync(run.ProjectId, cancellationToken);
        int totalPoints = tasks.Sum(task => task.Points ?? 0);

        ProjectBudget? budget = await budgetRepository.GetAsync(run.ProjectId, cancellationToken);

        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        var runDate = DateOnly.FromDateTime(run.CreatedAtUtc);
        if (runDate > today)
        {
            runDate = today;
        }

        var series = new List<BurnPoint>();
        for (DateOnly day = runDate; day <= today; day = day.AddDays(1))
        {
            DateOnly currentDay = day;
            int completedPointsByDay = totalPoints == 0
                ? 0
                : tasks
                    .Where(task => task.IsDone)
                    .Where(task => CompletionDay(task, runDate) <= currentDay)
                    .Sum(task => task.Points ?? 0);

            decimal actualSpend = totalPoints == 0 ? 0m : (decimal)completedPointsByDay / totalPoints * p50Total;
            series.Add(new BurnPoint(day, actualSpend));
        }

        return Result.Success(new BurnResponse(
            run.Id,
            budget?.Amount,
            run.Currency,
            series,
            new BurnForecast(p50Total, p90Total),
            dateTimeProvider.UtcNow));
    }

    private static DateOnly CompletionDay(CostEstimationTaskSummary task, DateOnly fallback) =>
        task.CompletedAtUtc is DateTime completedAtUtc ? DateOnly.FromDateTime(completedAtUtc) : fallback;
}

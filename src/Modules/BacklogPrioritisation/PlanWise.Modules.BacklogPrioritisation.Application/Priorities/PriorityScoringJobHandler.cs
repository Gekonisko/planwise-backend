using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Data;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities;

// Gathers the inputs, hands them to whichever IBacklogPrioritisationModel is registered, and
// persists the ordering that comes back. How an item earns its position lives behind the model seam.
//
// Two things deliberately stay here rather than in the model. Scope: only Status == "Backlog" tasks
// are prioritised — this is the backlog screen's ordering, not a general task ranking. And the
// current-position baseline, so the diff shown to the user is always measured against the order they
// actually had, whatever the model does.
//
// Risk scores come from RiskPrediction via IRiskInsightsService rather than a project reference. If
// that project has never had a forecast run the map is empty, and how to treat a missing score is
// the model's call (the scorecard falls back to a neutral 0.5).
public sealed class PriorityScoringJobHandler(
    IProjectTasksService projectTasksService,
    IRiskInsightsService riskInsightsService,
    IBacklogPrioritisationModel prioritisationModel,
    IPriorityRunRepository runRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IAsyncJobHandler
{
    public string JobType => "BacklogPrioritisation";

    public async Task<string> ExecuteAsync(Guid jobId, Guid projectId, CancellationToken cancellationToken)
    {
        IReadOnlyList<TaskInsightSummary> allTasks = await projectTasksService.GetInsightTasksAsync(projectId, cancellationToken);
        IReadOnlyDictionary<Guid, decimal> riskScores = await riskInsightsService.GetLatestRiskScoresAsync(projectId, cancellationToken);

        var backlogTasks = allTasks
            .Where(task => task.Status == "Backlog")
            .OrderBy(task => task.Rank)
            .ToList();

        var currentPositionByTaskId = backlogTasks
            .Select((task, index) => (task.TaskId, Position: index + 1))
            .ToDictionary(entry => entry.TaskId, entry => entry.Position);

        var input = new PrioritisationInput(projectId, backlogTasks, riskScores);
        PrioritisationResult prioritisation = await prioritisationModel.PrioritiseAsync(input, cancellationToken);

        var run = PriorityRun.Create(projectId, jobId, prioritisationModel.ModelName, dateTimeProvider.UtcNow);
        int proposedPosition = 1;
        foreach (PrioritisedTask item in prioritisation.Ordered)
        {
            // A model could name a task that isn't in the backlog it was handed; that item has no
            // current position to diff against, so it is skipped rather than persisted misleadingly.
            if (!currentPositionByTaskId.TryGetValue(item.TaskId, out int currentPosition))
            {
                continue;
            }

            run.AddItem(
                item.TaskId,
                item.TaskKey,
                currentPosition,
                proposedPosition,
                item.ValueScore,
                item.DependencyScore,
                item.ComplexityScore,
                item.RiskScore,
                item.Reason);
            proposedPosition++;
        }

        runRepository.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return $"/api/v1/projects/{projectId}/priorities";
    }
}

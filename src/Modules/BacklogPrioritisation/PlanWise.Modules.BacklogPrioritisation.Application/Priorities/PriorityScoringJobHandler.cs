using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Data;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

namespace PlanWise.Modules.BacklogPrioritisation.Application.Priorities;

// Scores only Status == "Backlog" tasks (not the whole project) — this is specifically the backlog
// prioritisation screen's ordering, not a general task ranking. Composes on top of RiskPrediction
// through IRiskInsightsService rather than a project reference: if that project has never had a risk
// forecast run, every task's risk component falls back to a neutral 0.5 (see PriorityScorer).
public sealed class PriorityScoringJobHandler(
    IProjectTasksService projectTasksService,
    IRiskInsightsService riskInsightsService,
    IPriorityRunRepository runRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IAsyncJobHandler
{
    private const string ModelVersion = "WeightedScorecard v1";

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

        IReadOnlyList<PriorityScorer.ScoredTask> scored = PriorityScorer.Score(backlogTasks, riskScores);

        var run = PriorityRun.Create(projectId, jobId, ModelVersion, dateTimeProvider.UtcNow);
        int proposedPosition = 1;
        foreach (PriorityScorer.ScoredTask scoredTask in scored)
        {
            run.AddItem(
                scoredTask.Task.TaskId,
                scoredTask.Task.Key,
                currentPositionByTaskId[scoredTask.Task.TaskId],
                proposedPosition,
                scoredTask.ValueScore,
                scoredTask.DependencyScore,
                scoredTask.ComplexityScore,
                scoredTask.RiskScore,
                scoredTask.Reason);
            proposedPosition++;
        }

        runRepository.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return $"/api/v1/projects/{projectId}/priorities";
    }
}

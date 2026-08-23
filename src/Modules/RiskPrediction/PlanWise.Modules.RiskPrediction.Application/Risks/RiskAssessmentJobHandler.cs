using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Data;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

// v1 model: a deterministic weighted heuristic (RiskScorer/SprintForecaster), not a trained
// statistical or ML model — this system has no historical slip-outcome data to fit or backtest
// against, which is reported honestly via Assumptions rather than implied by the "ML" label on the
// spec. TrainingWindowDays is reported as 0 for the same reason: there is no training window.
public sealed class RiskAssessmentJobHandler(
    IProjectTasksService projectTasksService,
    ISprintInsightsService sprintInsightsService,
    IProjectMembersService projectMembersService,
    IRiskAssessmentRunRepository runRepository,
    ITaskRiskAssessmentRepository taskRiskAssessmentRepository,
    ISprintForecastRepository sprintForecastRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IAsyncJobHandler
{
    private const string ModelVersion = "WeightedScorecard v1";
    private const int TrainingWindowDays = 0;

    private static readonly string[] Assumptions =
    [
        "Risk is scored by a deterministic weighted heuristic (due-date pressure, open blocking dependencies, missing assignee, task size), not a trained statistical or ML model.",
        "No historical delivery data (actual slip outcomes) exists yet in this system, so the heuristic cannot be calibrated or backtested against real results — weights are fixed, illustrative estimates.",
        "Sprint forecasts assume each member's configured capacity is available every day of the sprint at a constant rate; holidays, partial availability and mid-sprint scope changes are not modelled."
    ];

    public string JobType => "RiskForecast";

    public async Task<string> ExecuteAsync(Guid jobId, Guid projectId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        IReadOnlyList<TaskInsightSummary> tasks = await projectTasksService.GetInsightTasksAsync(projectId, cancellationToken);
        IReadOnlyList<SprintInsightSummary> sprints = await sprintInsightsService.GetSprintsAsync(projectId, cancellationToken);
        IReadOnlyList<ProjectMemberSummary> members = await projectMembersService.GetMembersAsync(projectId, cancellationToken);

        var run = RiskAssessmentRun.Create(projectId, jobId, ModelVersion, TrainingWindowDays, Assumptions, dateTimeProvider.UtcNow);
        runRepository.Add(run);

        var tasksById = tasks.ToDictionary(task => task.TaskId);
        var assessments = tasks
            .Where(task => task.Status != "Done")
            .Select(task =>
            {
                RiskScorer.ScoreResult score = RiskScorer.Score(task, tasksById, today);
                return TaskRiskAssessment.Create(
                    run.Id,
                    projectId,
                    task.TaskId,
                    task.Key,
                    score.Probability,
                    score.DayImpact,
                    score.Reason,
                    RiskMappings.SerializeFeatures(score.Features),
                    dateTimeProvider.UtcNow);
            })
            .ToList();
        taskRiskAssessmentRepository.AddRange(assessments);

        decimal teamCapacityPoints = members.Where(member => member.UserId is not null).Sum(member => member.Capacity);
        var forecasts = sprints
            .Where(sprint => sprint.State == "Active")
            .Select(sprint =>
            {
                var sprintTasks = tasks.Where(task => task.SprintId == sprint.SprintId).ToList();
                SprintForecaster.ForecastResult forecast = SprintForecaster.Forecast(sprint, sprintTasks, teamCapacityPoints, today);
                return SprintForecast.Create(
                    run.Id,
                    projectId,
                    sprint.SprintId,
                    forecast.CompletionProbability,
                    forecast.ExpectedPoints,
                    forecast.P50DeliveryDate,
                    forecast.P90DeliveryDate,
                    dateTimeProvider.UtcNow);
            })
            .ToList();
        sprintForecastRepository.AddRange(forecasts);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return $"/api/v1/projects/{projectId}/risks";
    }
}

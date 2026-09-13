using System.Globalization;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.RiskPrediction.Application.Abstractions;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Data;
using PlanWise.Modules.RiskPrediction.Application.Training;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Application.Risks;

// Gathers the inputs, hands them to whichever IRiskPredictionModel is registered, and persists what
// comes back. It deliberately knows nothing about how a risk number is arrived at — that lives
// behind the model seam, so replacing the heuristic with a trained model doesn't touch this file.
//
// The high-risk notification threshold stays here rather than in the model: who gets told about a
// prediction is a product decision, not part of predicting.
public sealed class RiskAssessmentJobHandler(
    IProjectTasksService projectTasksService,
    ISprintInsightsService sprintInsightsService,
    IProjectMembersService projectMembersService,
    IRiskPredictionModel riskPredictionModel,
    RiskTrainingDataRecorder trainingDataRecorder,
    IRiskAssessmentRunRepository runRepository,
    ITaskRiskAssessmentRepository taskRiskAssessmentRepository,
    ISprintForecastRepository sprintForecastRepository,
    INotificationPublisher notificationPublisher,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IAsyncJobHandler
{
    private const decimal HighRiskThreshold = 0.5m;

    public string JobType => "RiskForecast";

    public async Task<string> ExecuteAsync(Guid jobId, Guid projectId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        IReadOnlyList<TaskInsightSummary> tasks = await projectTasksService.GetInsightTasksAsync(projectId, cancellationToken);
        IReadOnlyList<SprintInsightSummary> sprints = await sprintInsightsService.GetSprintsAsync(projectId, cancellationToken);
        IReadOnlyList<ProjectMemberSummary> members = await projectMembersService.GetMembersAsync(projectId, cancellationToken);

        var input = new RiskPredictionInput(projectId, today, tasks, sprints, members);
        RiskPredictionResult prediction = await riskPredictionModel.PredictAsync(input, cancellationToken);

        var run = RiskAssessmentRun.Create(
            projectId,
            jobId,
            riskPredictionModel.ModelName,
            prediction.TrainingWindowDays,
            [.. prediction.Assumptions],
            dateTimeProvider.UtcNow);
        runRepository.Add(run);

        var assessments = prediction.TaskRisks
            .Select(risk => TaskRiskAssessment.Create(
                run.Id,
                projectId,
                risk.TaskId,
                risk.TaskKey,
                risk.ProbabilityOfSlip,
                risk.DayImpact,
                risk.Reason,
                RiskMappings.SerializeFeatures(risk.Features),
                dateTimeProvider.UtcNow))
            .ToList();
        taskRiskAssessmentRepository.AddRange(assessments);

        var forecasts = prediction.SprintForecasts
            .Select(forecast => SprintForecast.Create(
                run.Id,
                projectId,
                forecast.SprintId,
                forecast.CompletionProbability,
                forecast.ExpectedPoints,
                forecast.P50DeliveryDate,
                forecast.P90DeliveryDate,
                dateTimeProvider.UtcNow))
            .ToList();
        sprintForecastRepository.AddRange(forecasts);

        // Accumulate the labelled dataset a trained model will need. Capture must happen here, while
        // today's state is still in hand — the system stores current state only, so a feature vector
        // not written now cannot be reconstructed later. Resolution fills in outcomes on earlier
        // rows. All of it shares this run's transaction, and none of it is read by anything yet.
        await trainingDataRecorder.RecordAsync(
            run.Id, input, prediction, riskPredictionModel.ModelName, dateTimeProvider.UtcNow, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var tasksById = tasks.ToDictionary(task => task.TaskId);
        foreach (TaskRiskAssessment assessment in assessments.Where(assessment => assessment.ProbabilityOfSlip >= HighRiskThreshold))
        {
            // A model may score a task this handler didn't fetch (or one since deleted), so this
            // looks the task up defensively rather than indexing straight into the dictionary.
            if (!tasksById.TryGetValue(assessment.TaskId, out TaskInsightSummary? task) || task.AssigneeId is not Guid assigneeId)
            {
                continue;
            }

            string probabilityText = (assessment.ProbabilityOfSlip * 100).ToString("0", CultureInfo.InvariantCulture) + "%";
            await notificationPublisher.PublishAsync(
                assigneeId,
                projectId,
                "RiskFlag",
                $"{assessment.TaskKey} is at high risk of slipping ({probabilityText})",
                $"/api/v1/tasks/{assessment.TaskId}/risk",
                cancellationToken);
        }

        return $"/api/v1/projects/{projectId}/forecasts/latest";
    }
}

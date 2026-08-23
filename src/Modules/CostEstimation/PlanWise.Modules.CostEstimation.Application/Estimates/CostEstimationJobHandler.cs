using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.CostEstimation.Application.Abstractions;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Data;
using PlanWise.Modules.CostEstimation.Application.RateCard;
using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Application.Estimates;

// Persists every run (the thesis needs estimate-drift history), but caches on a hash of the inputs
// that actually shape the estimate (backlog + rate card) — if neither changed since the last run,
// this returns the existing run's location instead of spending tokens on an identical re-estimate.
public sealed class CostEstimationJobHandler(
    IProjectTasksService projectTasksService,
    IProjectAccessService projectAccessService,
    IRateCardProvider rateCardProvider,
    ICostEstimateRunRepository runRepository,
    ICostEstimationModel model,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IAsyncJobHandler
{
    private const string Currency = "USD";

    public string JobType => "CostEstimation";

    public async Task<string> ExecuteAsync(Guid jobId, Guid projectId, CancellationToken cancellationToken)
    {
        ProjectInfo? projectInfo = await projectAccessService.GetProjectInfoAsync(projectId, cancellationToken);
        IReadOnlyList<CostEstimationTaskSummary> tasks = await projectTasksService.GetCostEstimationTasksAsync(projectId, cancellationToken);
        IReadOnlyList<RoleRate> rateCard = rateCardProvider.GetRates();

        string inputHash = ComputeInputHash(tasks, rateCard);

        CostEstimateRun? latest = await runRepository.GetLatestForProjectAsync(projectId, cancellationToken);
        if (latest is not null && latest.InputHash == inputHash)
        {
            return $"/api/v1/cost-estimates/{latest.Id}";
        }

        var prompt = new CostEstimationPrompt(
            projectInfo?.Name ?? "Untitled project",
            projectInfo?.ClientName,
            Currency,
            tasks.Where(task => !task.IsDone).ToList(),
            rateCard);

        CostEstimateResult result = await model.EstimateAsync(prompt, cancellationToken);

        var run = CostEstimateRun.Create(
            projectId,
            jobId,
            model.ModelName,
            inputHash,
            Currency,
            CostEstimateMappings.SerializeResult(result),
            dateTimeProvider.UtcNow);

        runRepository.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return $"/api/v1/cost-estimates/{run.Id}";
    }

    private static string ComputeInputHash(IReadOnlyList<CostEstimationTaskSummary> tasks, IReadOnlyList<RoleRate> rateCard)
    {
        var canonical = new
        {
            Tasks = tasks
                .Where(task => !task.IsDone)
                .OrderBy(task => task.TaskId)
                .Select(task => new { task.TaskId, task.Title, task.Description, task.Priority, task.Points }),
            RateCard = rateCard.OrderBy(rate => rate.Role).Select(rate => new { rate.Role, rate.HourlyRate, rate.Currency })
        };

        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical));
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}

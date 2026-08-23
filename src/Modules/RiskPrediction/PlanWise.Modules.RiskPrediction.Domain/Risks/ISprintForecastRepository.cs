namespace PlanWise.Modules.RiskPrediction.Domain.Risks;

public interface ISprintForecastRepository
{
    Task<SprintForecast?> GetLatestForSprintAsync(Guid sprintId, CancellationToken cancellationToken = default);

    void AddRange(IEnumerable<SprintForecast> forecasts);
}

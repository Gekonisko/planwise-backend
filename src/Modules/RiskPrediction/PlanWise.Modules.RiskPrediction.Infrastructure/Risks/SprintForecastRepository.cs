using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.RiskPrediction.Domain.Risks;
using PlanWise.Modules.RiskPrediction.Infrastructure.Database;

namespace PlanWise.Modules.RiskPrediction.Infrastructure.Risks;

internal sealed class SprintForecastRepository(RiskPredictionDbContext dbContext) : ISprintForecastRepository
{
    public Task<SprintForecast?> GetLatestForSprintAsync(Guid sprintId, CancellationToken cancellationToken = default) =>
        dbContext.SprintForecasts
            .Where(forecast => forecast.SprintId == sprintId)
            .OrderByDescending(forecast => forecast.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public void AddRange(IEnumerable<SprintForecast> forecasts) => dbContext.SprintForecasts.AddRange(forecasts);
}

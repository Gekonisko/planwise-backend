namespace PlanWise.Common.Application.Abstractions;

public interface IProjectAccessService
{
    Task<bool> HasAccessAsync(Guid projectId, Guid userId, string? email, CancellationToken cancellationToken = default);

    Task<string?> GetKeyPrefixAsync(Guid projectId, CancellationToken cancellationToken = default);
}

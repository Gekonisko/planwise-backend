namespace PlanWise.Common.Application.Abstractions;

public interface IProjectAccessService
{
    Task<bool> HasAccessAsync(Guid projectId, Guid userId, string? email, CancellationToken cancellationToken = default);

    Task<string?> GetKeyPrefixAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectInfo?> GetProjectInfoAsync(Guid projectId, CancellationToken cancellationToken = default);
}

public sealed record ProjectInfo(string Name, string? ClientName);

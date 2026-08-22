using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Authentication;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.WorkspaceManagement.Application.Projects;
using PlanWise.Common.Domain;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Application.Projects.CreateProject;

internal sealed class CreateProjectCommandHandler(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<CreateProjectCommand, ProjectResponse>
{
    public async Task<Result<ProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid ownerId)
        {
            return Result.Failure<ProjectResponse>(ProjectErrors.NotFound(Guid.Empty));
        }

        if (await projectRepository.ExistsByKeyPrefixAsync(request.KeyPrefix, cancellationToken))
        {
            return Result.Failure<ProjectResponse>(ProjectErrors.KeyPrefixNotUnique(request.KeyPrefix));
        }

        var project = Project.Create(request.Name.Trim(), request.KeyPrefix, request.Process, ownerId, request.ClientName?.Trim());
        project.AddMember(ownerId, "", "Owner", 1m, 0m);
        projectRepository.Add(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success<ProjectResponse>(ProjectMappings.ToResponse(project));
    }
}
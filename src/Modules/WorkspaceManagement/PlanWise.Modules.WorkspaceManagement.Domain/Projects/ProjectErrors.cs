using PlanWise.Common.Domain;

namespace PlanWise.Modules.WorkspaceManagement.Domain.Projects;

public static class ProjectErrors
{
    public static Error NotFound(Guid projectId) =>
        Error.NotFound("Project.NotFound", $"The project with identifier {projectId} was not found");

    public static Error KeyPrefixNotUnique(string keyPrefix) =>
        Error.Conflict("Project.KeyPrefixNotUnique", $"The project key prefix {keyPrefix} is already in use");

    public static Error MemberNotFound(Guid memberId) =>
        Error.NotFound("Project.MemberNotFound", $"The member with identifier {memberId} was not found");

    public static Error MemberAlreadyExists(Guid userId) =>
        Error.Conflict("Project.MemberAlreadyExists", $"The user with identifier {userId} is already a member");

    public static Error MemberAlreadyInvited(string email) =>
        Error.Conflict("Project.MemberAlreadyInvited", $"{email} has already been added or invited to this project");
}
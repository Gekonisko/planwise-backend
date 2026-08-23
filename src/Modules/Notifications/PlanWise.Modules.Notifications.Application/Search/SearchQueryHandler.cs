using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Notifications.Application.Abstractions.Authentication;
using PlanWise.Modules.Notifications.Domain;

namespace PlanWise.Modules.Notifications.Application.Search;

// No search index: this queries every project the user can access (name/key match) plus, within
// each of those projects, every task (title/key match) — one round trip per accessible project. Fine
// at the scale this system runs at today; a real cross-project search index would replace this
// entirely rather than optimise it, so the N+1 is left as a stated simplification, not micro-tuned.
internal sealed class SearchQueryHandler(
    IProjectAccessService projectAccessService,
    IProjectTasksService projectTasksService,
    IUserContext userContext)
    : IQueryHandler<SearchQuery, SearchResponse>
{
    public async Task<Result<SearchResponse>> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        if (userContext.UserId is not Guid userId)
        {
            return Result.Failure<SearchResponse>(NotificationErrors.NotAuthenticated());
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return Result.Success(new SearchResponse([]));
        }

        IReadOnlyList<ProjectSearchSummary> projects = await projectAccessService.GetAccessibleProjectsAsync(userId, cancellationToken);

        var results = new List<SearchResultResponse>();

        results.AddRange(projects
            .Where(project =>
                project.Name.Contains(request.Text, StringComparison.OrdinalIgnoreCase) ||
                project.KeyPrefix.Contains(request.Text, StringComparison.OrdinalIgnoreCase))
            .Select(project => new SearchResultResponse(
                "project", project.ProjectId, project.ProjectId, project.Name, project.KeyPrefix,
                $"/api/v1/projects/{project.ProjectId}")));

        foreach (ProjectSearchSummary project in projects)
        {
            IReadOnlyList<TaskSearchSummary> tasks = await projectTasksService.SearchTasksAsync(project.ProjectId, request.Text, cancellationToken);
            results.AddRange(tasks.Select(task => new SearchResultResponse(
                "task", task.TaskId, task.ProjectId, task.Title, task.Key, $"/api/v1/tasks/{task.TaskId}")));
        }

        return Result.Success(new SearchResponse(results));
    }
}

namespace PlanWise.Modules.Notifications.Application.Search;

public sealed record SearchResultResponse(string Type, Guid Id, Guid? ProjectId, string Title, string? Subtitle, string Link);

public sealed record SearchResponse(IReadOnlyList<SearchResultResponse> Results);

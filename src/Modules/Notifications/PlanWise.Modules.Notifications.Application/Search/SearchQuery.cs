using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Notifications.Application.Search;

public sealed record SearchQuery(string Text) : IQuery<SearchResponse>;

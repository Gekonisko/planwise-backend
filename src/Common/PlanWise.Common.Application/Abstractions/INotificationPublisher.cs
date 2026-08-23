namespace PlanWise.Common.Application.Abstractions;

// Owned by the Notifications module. Any module that detects a bell-worthy event (RiskPrediction on
// a newly-flagged high-risk task, Delivery on an @mention or a sprint start/complete) calls this
// directly rather than going through a cross-module domain-event bus — same narrow-contract pattern
// as every other Common abstraction, and consistent with the "trimmed Outbox, in-process only" design
// (see DomainEvent/OutboxProcessor): this is a synchronous fire-and-persist call, not a queued message.
public interface INotificationPublisher
{
    Task PublishAsync(
        Guid userId,
        Guid? projectId,
        string type,
        string message,
        string? link,
        CancellationToken cancellationToken = default);
}

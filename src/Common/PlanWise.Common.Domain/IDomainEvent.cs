using MediatR;

namespace PlanWise.Common.Domain;

public interface IDomainEvent : INotification
{
    Guid Id { get; }
    DateTime OccurredOnUtc { get; }
}

using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Sprints;

public sealed class Sprint : Entity
{
    private Sprint()
    {
    }

    private Sprint(Guid projectId, string name, string? goal, DateOnly startDate, DateOnly endDate)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Name = name;
        Goal = goal;
        StartDate = startDate;
        EndDate = endDate;
        State = SprintState.Planned;
    }

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public string? Goal { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public SprintState State { get; private set; }

    public static Sprint Create(Guid projectId, string name, string? goal, DateOnly startDate, DateOnly endDate) =>
        new(projectId, name, goal, startDate, endDate);

    public void Update(string? name, string? goal, DateOnly? startDate, DateOnly? endDate)
    {
        if (name is not null)
        {
            Name = name;
        }

        if (goal is not null)
        {
            Goal = goal;
        }

        if (startDate is not null)
        {
            StartDate = startDate.Value;
        }

        if (endDate is not null)
        {
            EndDate = endDate.Value;
        }
    }

    public Result Start()
    {
        if (State != SprintState.Planned)
        {
            return Result.Failure(SprintErrors.InvalidStateTransition(Id, "start"));
        }

        State = SprintState.Active;
        Raise(new SprintStartedDomainEvent(Id, ProjectId, Name));
        return Result.Success();
    }

    public Result Complete()
    {
        if (State != SprintState.Active)
        {
            return Result.Failure(SprintErrors.InvalidStateTransition(Id, "complete"));
        }

        State = SprintState.Completed;
        Raise(new SprintCompletedDomainEvent(Id, ProjectId, Name));
        return Result.Success();
    }
}

public enum SprintState
{
    Planned,
    Active,
    Completed
}

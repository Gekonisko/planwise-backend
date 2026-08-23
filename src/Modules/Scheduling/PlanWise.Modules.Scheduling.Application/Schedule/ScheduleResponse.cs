namespace PlanWise.Modules.Scheduling.Application.Schedule;

public sealed record ScheduleTaskRow(
    Guid TaskId,
    string Key,
    string Title,
    bool IsDone,
    DateOnly StartDate,
    DateOnly EndDate,
    int SlackDays,
    bool IsCritical,
    bool IsManuallyScheduled,
    IReadOnlyList<Guid> PredecessorTaskIds);

public sealed record ScheduleMilestoneRow(Guid MilestoneId, string Name, DateOnly DueDate, string Status);

public sealed record ScheduleResponse(
    IReadOnlyList<ScheduleTaskRow> Tasks,
    IReadOnlyList<ScheduleMilestoneRow> Milestones,
    IReadOnlyList<Guid> CriticalPathTaskIds);

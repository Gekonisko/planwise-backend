using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Scheduling.Application.Schedule.ValidateSchedule;

public sealed record ProposedMove(Guid TaskId, DateOnly StartDate, DateOnly EndDate);

public sealed record ScheduleViolation(Guid TaskId, string Reason);

public sealed record ScheduleValidationResponse(IReadOnlyList<ScheduleViolation> Violations);

public sealed record ValidateScheduleCommand(Guid ProjectId, IReadOnlyList<ProposedMove> Moves) : ICommand<ScheduleValidationResponse>;

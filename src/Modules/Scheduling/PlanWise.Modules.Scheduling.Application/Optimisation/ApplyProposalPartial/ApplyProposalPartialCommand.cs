using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Scheduling.Application.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.ApplyProposalPartial;

public sealed record ApplyProposalPartialCommand(Guid ProposalId, IReadOnlyList<Guid> AssignmentIds) : ICommand<ScheduleResponse>;

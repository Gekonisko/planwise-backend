using PlanWise.Common.Application.Messaging;
using PlanWise.Modules.Scheduling.Application.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.ApplyProposal;

public sealed record ApplyProposalCommand(Guid ProposalId) : ICommand<ScheduleResponse>;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.GetLatestProposal;

public sealed record GetLatestProposalQuery(Guid ProjectId) : IQuery<ProposalResponse>;

using PlanWise.Common.Application.Messaging;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.GetProposalExplanation;

public sealed record GetProposalExplanationQuery(Guid ProposalId) : IQuery<ProposalExplanationResponse>;

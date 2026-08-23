using MediatR;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Application.Schedule;
using PlanWise.Modules.Scheduling.Domain.Optimisation;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.ApplyProposal;

internal sealed class ApplyProposalCommandHandler(
    IScheduleProposalRepository proposalRepository,
    IProjectAccessService projectAccessService,
    IProjectTasksService projectTasksService,
    IUnitOfWork unitOfWork,
    ISender sender,
    IUserContext userContext)
    : ICommandHandler<ApplyProposalCommand, ScheduleResponse>
{
    public Task<Result<ScheduleResponse>> Handle(ApplyProposalCommand request, CancellationToken cancellationToken) =>
        ProposalApplier.ApplyAsync(
            request.ProposalId,
            null,
            proposalRepository,
            projectAccessService,
            projectTasksService,
            unitOfWork,
            sender,
            userContext,
            cancellationToken);
}

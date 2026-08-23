using MediatR;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Messaging;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Application.Schedule;
using PlanWise.Modules.Scheduling.Domain.Optimisation;

namespace PlanWise.Modules.Scheduling.Application.Optimisation.ApplyProposalPartial;

internal sealed class ApplyProposalPartialCommandHandler(
    IScheduleProposalRepository proposalRepository,
    IProjectAccessService projectAccessService,
    IProjectTasksService projectTasksService,
    IUnitOfWork unitOfWork,
    ISender sender,
    IUserContext userContext)
    : ICommandHandler<ApplyProposalPartialCommand, ScheduleResponse>
{
    public Task<Result<ScheduleResponse>> Handle(ApplyProposalPartialCommand request, CancellationToken cancellationToken) =>
        ProposalApplier.ApplyAsync(
            request.ProposalId,
            request.AssignmentIds,
            proposalRepository,
            projectAccessService,
            projectTasksService,
            unitOfWork,
            sender,
            userContext,
            cancellationToken);
}

using MediatR;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Application.Abstractions.Authentication;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Application.Schedule;
using PlanWise.Modules.Scheduling.Application.Schedule.GetSchedule;
using PlanWise.Modules.Scheduling.Domain.Optimisation;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Application.Optimisation;

// Shared by ApplyProposalCommandHandler and ApplyProposalPartialCommandHandler — the only difference
// between "apply" and "apply-partial" is whether assignmentIds is null (everything) or a specific
// subset, so both funnel through here rather than duplicating the lookup/access-check/propagate flow.
internal static class ProposalApplier
{
    public static async Task<Result<ScheduleResponse>> ApplyAsync(
        Guid proposalId,
        IReadOnlyList<Guid>? assignmentIds,
        IScheduleProposalRepository proposalRepository,
        IProjectAccessService projectAccessService,
        IProjectTasksService projectTasksService,
        IUnitOfWork unitOfWork,
        ISender sender,
        IUserContext userContext,
        CancellationToken cancellationToken)
    {
        ScheduleProposal? proposal = await proposalRepository.GetAsync(proposalId, cancellationToken);
        if (proposal is null)
        {
            return Result.Failure<ScheduleResponse>(ScheduleErrors.ProposalNotFound(proposalId));
        }

        if (userContext.UserId is not Guid userId ||
            !await projectAccessService.HasAccessAsync(proposal.ProjectId, userId, userContext.Email, cancellationToken))
        {
            return Result.Failure<ScheduleResponse>(ScheduleErrors.ProposalNotFound(proposalId));
        }

        Result<IReadOnlyList<ProposedAssignment>> applyResult = proposal.Apply(assignmentIds);
        if (applyResult.IsFailure)
        {
            return Result.Failure<ScheduleResponse>(applyResult.Error);
        }

        foreach (ProposedAssignment assignment in applyResult.Value)
        {
            await projectTasksService.AssignTaskAsync(assignment.TaskId, assignment.ProposedAssigneeId, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await sender.Send(new GetScheduleQuery(proposal.ProjectId), cancellationToken);
    }
}

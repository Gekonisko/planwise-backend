using PlanWise.Common.Application.Abstractions;
using PlanWise.Common.Application.Clock;
using PlanWise.Modules.Scheduling.Application.Abstractions;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Domain.Optimisation;

namespace PlanWise.Modules.Scheduling.Application.Optimisation;

// Gathers the input, hands it to whichever optimiser is registered, and persists the proposal. It
// holds no opinion about how assignments are chosen — that moved behind IScheduleOptimisationModel,
// so swapping a greedy heuristic for a CP-SAT solver touches the registration and nothing here.
public sealed class ScheduleOptimisationJobHandler(
    IProjectTasksService projectTasksService,
    IProjectMembersService projectMembersService,
    IScheduleOptimisationModel optimisationModel,
    IScheduleProposalRepository proposalRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : IAsyncJobHandler
{
    public string JobType => "ScheduleOptimisation";

    public async Task<string> ExecuteAsync(Guid jobId, Guid projectId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ScheduleTaskSummary> tasks = await projectTasksService.GetScheduleTasksAsync(projectId, cancellationToken);
        IReadOnlyList<ProjectMemberSummary> members = await projectMembersService.GetMembersAsync(projectId, cancellationToken);

        var input = new ScheduleOptimisationInput(
            projectId, DateOnly.FromDateTime(dateTimeProvider.UtcNow), tasks, members);

        ScheduleOptimisationResult result = await optimisationModel.OptimiseAsync(input, cancellationToken);

        var proposal = ScheduleProposal.Create(
            projectId,
            jobId,
            result.ModelName,
            result.Objective,
            [.. result.ConstraintsHonoured],
            [.. result.ConstraintsRelaxed],
            result.ExpectedGain,
            dateTimeProvider.UtcNow);

        foreach (ProposedTaskAssignment assignment in result.Assignments)
        {
            proposal.AddAssignment(
                assignment.TaskId,
                assignment.TaskKey,
                assignment.CurrentAssigneeId,
                assignment.ProposedAssigneeId,
                assignment.ProposedAssigneeEmail);
        }

        proposalRepository.Add(proposal);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return $"/api/v1/schedule/proposals/{proposal.Id}";
    }
}

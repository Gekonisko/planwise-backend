using PlanWise.Modules.Scheduling.Domain.Optimisation;

namespace PlanWise.Modules.Scheduling.Application.Optimisation;

internal static class ProposalMappings
{
    public static ProposalResponse ToResponse(ScheduleProposal proposal) =>
        new(
            proposal.Id,
            proposal.ProjectId,
            proposal.JobId,
            proposal.Status.ToString(),
            proposal.Assignments.Select(ToResponse).ToList(),
            proposal.CreatedAtUtc);

    public static ProposedAssignmentResponse ToResponse(ProposedAssignment assignment) =>
        new(
            assignment.Id,
            assignment.TaskId,
            assignment.TaskKey,
            assignment.CurrentAssigneeId,
            assignment.ProposedAssigneeId,
            assignment.ProposedAssigneeEmail,
            assignment.IsApplied);

    public static ProposalExplanationResponse ToExplanationResponse(ScheduleProposal proposal) =>
        new(
            proposal.Id,
            proposal.ModelName,
            proposal.Objective,
            proposal.ConstraintsHonoured,
            proposal.ConstraintsRelaxed,
            proposal.ExpectedGain,
            proposal.CreatedAtUtc);
}

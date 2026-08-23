using PlanWise.Common.Domain;

namespace PlanWise.Modules.Scheduling.Domain.Optimisation;

public sealed class ProposedAssignment : Entity
{
    private ProposedAssignment()
    {
    }

    private ProposedAssignment(
        Guid proposalId,
        Guid taskId,
        string taskKey,
        Guid? currentAssigneeId,
        Guid proposedAssigneeId,
        string proposedAssigneeEmail)
    {
        Id = Guid.NewGuid();
        ProposalId = proposalId;
        TaskId = taskId;
        TaskKey = taskKey;
        CurrentAssigneeId = currentAssigneeId;
        ProposedAssigneeId = proposedAssigneeId;
        ProposedAssigneeEmail = proposedAssigneeEmail;
        IsApplied = false;
    }

    public Guid ProposalId { get; private set; }
    public Guid TaskId { get; private set; }
    public string TaskKey { get; private set; }
    public Guid? CurrentAssigneeId { get; private set; }
    public Guid ProposedAssigneeId { get; private set; }
    public string ProposedAssigneeEmail { get; private set; }
    public bool IsApplied { get; private set; }

    public static ProposedAssignment Create(
        Guid proposalId,
        Guid taskId,
        string taskKey,
        Guid? currentAssigneeId,
        Guid proposedAssigneeId,
        string proposedAssigneeEmail) =>
        new(proposalId, taskId, taskKey, currentAssigneeId, proposedAssigneeId, proposedAssigneeEmail);

    public void MarkApplied() => IsApplied = true;
}

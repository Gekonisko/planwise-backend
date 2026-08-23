using PlanWise.Common.Domain;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Domain.Optimisation;

public sealed class ScheduleProposal : Entity
{
    private readonly List<ProposedAssignment> assignments = [];

    private ScheduleProposal()
    {
    }

    private ScheduleProposal(
        Guid projectId,
        Guid jobId,
        string modelName,
        string objective,
        string[] constraintsHonoured,
        string[] constraintsRelaxed,
        string expectedGain,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        JobId = jobId;
        ModelName = modelName;
        Objective = objective;
        ConstraintsHonoured = constraintsHonoured;
        ConstraintsRelaxed = constraintsRelaxed;
        ExpectedGain = expectedGain;
        CreatedAtUtc = createdAtUtc;
        Status = ProposalStatus.Pending;
    }

    public Guid ProjectId { get; private set; }
    public Guid JobId { get; private set; }
    public string ModelName { get; private set; }
    public string Objective { get; private set; }
    public string[] ConstraintsHonoured { get; private set; } = [];
    public string[] ConstraintsRelaxed { get; private set; } = [];
    public string ExpectedGain { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public ProposalStatus Status { get; private set; }
    public IReadOnlyCollection<ProposedAssignment> Assignments => assignments;

    public static ScheduleProposal Create(
        Guid projectId,
        Guid jobId,
        string modelName,
        string objective,
        string[] constraintsHonoured,
        string[] constraintsRelaxed,
        string expectedGain,
        DateTime createdAtUtc) =>
        new(projectId, jobId, modelName, objective, constraintsHonoured, constraintsRelaxed, expectedGain, createdAtUtc);

    public ProposedAssignment AddAssignment(
        Guid taskId,
        string taskKey,
        Guid? currentAssigneeId,
        Guid proposedAssigneeId,
        string proposedAssigneeEmail)
    {
        var assignment = ProposedAssignment.Create(Id, taskId, taskKey, currentAssigneeId, proposedAssigneeId, proposedAssigneeEmail);
        assignments.Add(assignment);
        return assignment;
    }

    // Returns the assignments newly marked applied by this call (empty if assignmentIds were all
    // already applied), so the caller knows exactly which ones still need pushing to Delivery —
    // makes a repeated apply call on an already-applied proposal a safe no-op.
    public Result<IReadOnlyList<ProposedAssignment>> Apply(IReadOnlyList<Guid>? assignmentIds)
    {
        List<ProposedAssignment> candidates = assignmentIds is null
            ? assignments.ToList()
            : assignments.Where(assignment => assignmentIds.Contains(assignment.Id)).ToList();

        if (assignmentIds is not null && candidates.Count != assignmentIds.Count)
        {
            return Result.Failure<IReadOnlyList<ProposedAssignment>>(ScheduleErrors.InvalidAssignmentSet(Id));
        }

        var newlyApplied = candidates.Where(assignment => !assignment.IsApplied).ToList();
        foreach (ProposedAssignment assignment in newlyApplied)
        {
            assignment.MarkApplied();
        }

        Status = assignments.TrueForAll(assignment => assignment.IsApplied) ? ProposalStatus.Applied : ProposalStatus.PartiallyApplied;
        return Result.Success<IReadOnlyList<ProposedAssignment>>(newlyApplied);
    }
}

public enum ProposalStatus
{
    Pending,
    Applied,
    PartiallyApplied
}

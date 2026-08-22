namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class ProjectTaskSequence
{
    private ProjectTaskSequence()
    {
    }

    private ProjectTaskSequence(Guid projectId)
    {
        ProjectId = projectId;
        NextNumber = 1;
    }

    public Guid ProjectId { get; private set; }
    public int NextNumber { get; private set; }

    public static ProjectTaskSequence Create(Guid projectId) => new(projectId);

    public int TakeNext()
    {
        int number = NextNumber;
        NextNumber++;
        return number;
    }
}

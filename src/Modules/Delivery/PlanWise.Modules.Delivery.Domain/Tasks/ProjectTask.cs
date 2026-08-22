using PlanWise.Common.Domain;

namespace PlanWise.Modules.Delivery.Domain.Tasks;

public sealed class ProjectTask : Entity
{
    private readonly List<Subtask> subtasks = [];
    private readonly List<TaskComment> comments = [];
    private readonly List<TaskLink> links = [];
    private readonly List<TaskLabel> labels = [];

    private ProjectTask()
    {
    }

    private ProjectTask(
        Guid projectId,
        string key,
        string title,
        string? description,
        TaskPriority priority,
        int? points,
        Guid? assigneeId,
        DateOnly? dueDate,
        decimal rank)
    {
        Id = Guid.NewGuid();
        ProjectId = projectId;
        Key = key;
        Title = title;
        Description = description;
        Status = ProjectTaskStatus.Backlog;
        Priority = priority;
        Points = points;
        AssigneeId = assigneeId;
        DueDate = dueDate;
        Rank = rank;
    }

    public Guid ProjectId { get; private set; }
    public Guid? SprintId { get; private set; }
    public string Key { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public int? Points { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public decimal Rank { get; private set; }
    public int? BusinessValue { get; private set; }
    public IReadOnlyCollection<Subtask> Subtasks => subtasks;
    public IReadOnlyCollection<TaskComment> Comments => comments;
    public IReadOnlyCollection<TaskLink> Links => links;
    public IReadOnlyCollection<TaskLabel> Labels => labels;

    public static ProjectTask Create(
        Guid projectId,
        string key,
        string title,
        string? description,
        TaskPriority priority,
        int? points,
        Guid? assigneeId,
        DateOnly? dueDate,
        decimal rank) =>
        new(projectId, key, title, description, priority, points, assigneeId, dueDate, rank);

    public void Update(
        string? title,
        string? description,
        TaskPriority? priority,
        int? points,
        Guid? assigneeId,
        DateOnly? dueDate,
        Guid? sprintId)
    {
        if (title is not null)
        {
            Title = title;
        }

        if (description is not null)
        {
            Description = description;
        }

        if (priority is not null)
        {
            Priority = priority.Value;
        }

        if (points is not null)
        {
            Points = points;
        }

        if (assigneeId is not null)
        {
            AssigneeId = assigneeId;
        }

        if (dueDate is not null)
        {
            DueDate = dueDate;
        }

        if (sprintId is not null)
        {
            SprintId = sprintId;
        }
    }

    // Returns only the newly created labels: EF Core cannot reliably detect a new child entity added
    // to an already-tracked aggregate's collection as Added (vs. Modified), so the caller must
    // explicitly Add() these when the task itself is already tracked. See IProjectTaskRepository.AddLabels.
    public IReadOnlyList<TaskLabel> ReplaceLabels(IReadOnlyList<Guid> labelIds)
    {
        labels.Clear();
        var newLabels = labelIds.Distinct().Select(labelId => TaskLabel.Create(Id, labelId)).ToList();
        labels.AddRange(newLabels);
        return newLabels;
    }

    public void Move(ProjectTaskStatus status, decimal rank)
    {
        Status = status;
        Rank = rank;
    }

    public void Reorder(decimal rank) => Rank = rank;

    public void SetBusinessValue(int businessValue) => BusinessValue = businessValue;

    public Subtask AddSubtask(string title)
    {
        var subtask = Subtask.Create(Id, title);
        subtasks.Add(subtask);
        return subtask;
    }

    public Result UpdateSubtask(Guid subtaskId, string? title, bool? isDone)
    {
        Subtask? subtask = subtasks.SingleOrDefault(s => s.Id == subtaskId);
        if (subtask is null)
        {
            return Result.Failure(TaskErrors.SubtaskNotFound(subtaskId));
        }

        subtask.Update(title, isDone);
        return Result.Success();
    }

    public void RemoveSubtask(Guid subtaskId) => subtasks.RemoveAll(s => s.Id == subtaskId);

    public TaskComment AddComment(Guid authorUserId, string body, DateTime createdAtUtc)
    {
        var comment = TaskComment.Create(Id, authorUserId, body, createdAtUtc);
        comments.Add(comment);
        return comment;
    }

    public TaskLink AddLink(Guid linkedTaskId, TaskLinkType type)
    {
        var link = TaskLink.Create(Id, linkedTaskId, type);
        links.Add(link);
        return link;
    }

    public void RemoveLink(Guid linkId) => links.RemoveAll(l => l.Id == linkId);
}

public enum ProjectTaskStatus
{
    Backlog,
    Todo,
    InProgress,
    Done
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

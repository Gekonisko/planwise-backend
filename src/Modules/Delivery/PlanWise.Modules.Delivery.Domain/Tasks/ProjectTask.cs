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
    public DateTime? CompletedAtUtc { get; private set; }
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
        decimal rank)
    {
        var task = new ProjectTask(projectId, key, title, description, priority, points, assigneeId, dueDate, rank);
        task.Raise(new ProjectTaskCreatedDomainEvent(task.Id, task.ProjectId, task.Key, task.Title));
        return task;
    }

    // The nullable fields take Optional<T> rather than plain nullables so that "field omitted" and
    // "field explicitly set to null" stay distinguishable — clearing a sprint, assignee, due date or
    // estimate is a real user action, and treating null as "no change" made all four impossible.
    public void Update(
        string? title,
        string? description,
        TaskPriority? priority,
        Optional<int?> points,
        Optional<Guid?> assigneeId,
        Optional<DateOnly?> dueDate,
        Optional<Guid?> sprintId)
    {
        bool changed = title is not null || description is not null || priority is not null ||
                       points.IsSet || assigneeId.IsSet || dueDate.IsSet || sprintId.IsSet;

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

        if (points.IsSet)
        {
            Points = points.Value;
        }

        if (assigneeId.IsSet)
        {
            AssigneeId = assigneeId.Value;
        }

        if (dueDate.IsSet)
        {
            DueDate = dueDate.Value;
        }

        if (sprintId.IsSet)
        {
            ApplySprint(sprintId.Value);
        }

        if (changed)
        {
            Raise(new ProjectTaskUpdatedDomainEvent(Id, ProjectId, Key));
        }
    }

    // Sprint membership and board status are two views of the same fact: a task committed to a sprint
    // belongs on the board, and a task pulled back to the product backlog does not. Without this
    // coupling a task could sit in a sprint while still holding Status.Backlog, and the board -- which
    // only renders the three post-backlog columns -- would show it nowhere at all.
    //
    // Only the two safe transitions are automated. Promotion happens from Backlog alone, so work
    // already in progress or done keeps its column when it moves between sprints; demotion happens
    // from the first column alone, so pulling a started or finished task out of a sprint never
    // discards that progress.
    private void ApplySprint(Guid? sprintId)
    {
        bool joiningSprint = sprintId is not null && SprintId != sprintId;
        bool leavingSprint = sprintId is null && SprintId is not null;

        SprintId = sprintId;

        if (joiningSprint && Status == ProjectTaskStatus.Backlog)
        {
            Status = ProjectTaskStatus.Todo;
        }
        else if (leavingSprint && Status == ProjectTaskStatus.Todo)
        {
            Status = ProjectTaskStatus.Backlog;
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

    // completedAtUtc is stamped the moment a task first lands on Done and cleared if it's ever moved
    // back off Done — the only historical signal the burndown chart has for "when did this task's
    // points actually leave the remaining total," since no separate status-change history exists.
    public void Move(ProjectTaskStatus status, decimal rank, DateTime nowUtc)
    {
        if (Status != status)
        {
            Raise(new ProjectTaskMovedDomainEvent(Id, ProjectId, Key, Title, Status, status));
        }

        CompletedAtUtc = status == ProjectTaskStatus.Done ? CompletedAtUtc ?? nowUtc : null;
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
        Raise(new ProjectTaskCommentAddedDomainEvent(Id, ProjectId, Key, authorUserId, body));
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

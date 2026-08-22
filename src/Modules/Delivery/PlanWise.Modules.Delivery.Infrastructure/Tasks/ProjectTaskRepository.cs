using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.Delivery.Domain.Tasks;
using PlanWise.Modules.Delivery.Infrastructure.Database;

namespace PlanWise.Modules.Delivery.Infrastructure.Tasks;

internal sealed class ProjectTaskRepository(DeliveryDbContext dbContext) : IProjectTaskRepository
{
    public Task<ProjectTask?> GetAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Include(dbContext.Tasks).SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken);

    public async Task<IReadOnlyList<ProjectTask>> GetByIdsAsync(Guid projectId, IReadOnlyList<Guid> taskIds, CancellationToken cancellationToken = default) =>
        await Include(dbContext.Tasks)
            .Where(task => task.ProjectId == projectId && taskIds.Contains(task.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ProjectTask>> GetByProjectAsync(
        Guid projectId,
        Guid? sprintId,
        ProjectTaskStatus? status,
        Guid? assigneeId,
        Guid? labelId,
        string? searchText,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ProjectTask> query = Include(dbContext.Tasks).Where(task => task.ProjectId == projectId);

        if (sprintId is not null)
        {
            query = query.Where(task => task.SprintId == sprintId);
        }

        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        if (assigneeId is not null)
        {
            query = query.Where(task => task.AssigneeId == assigneeId);
        }

        if (labelId is not null)
        {
            query = query.Where(task => task.Labels.Any(label => label.LabelId == labelId));
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(task => EF.Functions.ILike(task.Title, $"%{searchText}%") ||
                task.Description != null && EF.Functions.ILike(task.Description, $"%{searchText}%"));
        }

        return await query.OrderBy(task => task.Rank).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectTask>> GetByStatusAsync(Guid projectId, ProjectTaskStatus status, CancellationToken cancellationToken = default) =>
        await Include(dbContext.Tasks)
            .Where(task => task.ProjectId == projectId && task.Status == status)
            .OrderBy(task => task.Rank)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetMaxRankAsync(Guid projectId, ProjectTaskStatus status, CancellationToken cancellationToken = default)
    {
        IQueryable<ProjectTask> tasks = dbContext.Tasks.Where(task => task.ProjectId == projectId && task.Status == status);
        return await tasks.AnyAsync(cancellationToken)
            ? await tasks.MaxAsync(task => task.Rank, cancellationToken)
            : 0m;
    }

    public async Task<int> GetNextTaskNumberAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        ProjectTaskSequence? sequence = await dbContext.TaskSequences.SingleOrDefaultAsync(s => s.ProjectId == projectId, cancellationToken);
        if (sequence is null)
        {
            sequence = ProjectTaskSequence.Create(projectId);
            dbContext.TaskSequences.Add(sequence);
        }

        int number = sequence.TakeNext();

        // Committed separately so the reserved number survives even if the caller's own SaveChanges
        // later rolls back for an unrelated reason; gaps in task numbering are expected and harmless.
        await dbContext.SaveChangesAsync(cancellationToken);
        return number;
    }

    public void Add(ProjectTask task) => dbContext.Tasks.Add(task);

    public void Remove(ProjectTask task) => dbContext.Tasks.Remove(task);

    public void AddSubtask(Subtask subtask) => dbContext.Add(subtask);

    public void AddComment(TaskComment comment) => dbContext.Add(comment);

    public void AddLink(TaskLink link) => dbContext.Add(link);

    public void AddLabels(IEnumerable<TaskLabel> labels)
    {
        foreach (TaskLabel label in labels)
        {
            dbContext.Add(label);
        }
    }

    private static IQueryable<ProjectTask> Include(IQueryable<ProjectTask> tasks) =>
        tasks
            .Include(task => task.Subtasks)
            .Include(task => task.Comments)
            .Include(task => task.Links)
            .Include(task => task.Labels);
}

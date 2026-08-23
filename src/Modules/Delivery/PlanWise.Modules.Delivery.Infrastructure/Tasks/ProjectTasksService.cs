using Microsoft.EntityFrameworkCore;
using PlanWise.Common.Application.Abstractions;
using PlanWise.Modules.Delivery.Domain.Tasks;
using PlanWise.Modules.Delivery.Infrastructure.Database;

namespace PlanWise.Modules.Delivery.Infrastructure.Tasks;

internal sealed class ProjectTasksService(DeliveryDbContext dbContext) : IProjectTasksService
{
    public async Task<IReadOnlyList<ScheduleTaskSummary>> GetScheduleTasksAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        List<ProjectTask> tasks = await dbContext.Tasks
            .Include(task => task.Links)
            .Where(task => task.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        return tasks.Select(task => ToSummary(task, tasks)).ToList();
    }

    public async Task<ScheduleTaskSummary?> GetScheduleTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        ProjectTask? task = await dbContext.Tasks
            .Include(t => t.Links)
            .SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return null;
        }

        List<ProjectTask> projectTasks = await dbContext.Tasks
            .Include(t => t.Links)
            .Where(t => t.ProjectId == task.ProjectId)
            .ToListAsync(cancellationToken);

        return ToSummary(task, projectTasks);
    }

    public async Task<bool> AssignTaskAsync(Guid taskId, Guid assigneeId, CancellationToken cancellationToken = default)
    {
        ProjectTask? task = await dbContext.Tasks.SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return false;
        }

        task.Update(title: null, description: null, priority: null, points: null, assigneeId: assigneeId, dueDate: null, sprintId: null);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<CostEstimationTaskSummary>> GetCostEstimationTasksAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        List<ProjectTask> tasks = await dbContext.Tasks
            .Where(task => task.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        return tasks
            .Select(task => new CostEstimationTaskSummary(
                task.Id,
                task.Key,
                task.Title,
                task.Description,
                task.Priority.ToString(),
                task.Points,
                task.Status == ProjectTaskStatus.Done))
            .ToList();
    }

    public async Task<IReadOnlyList<TaskInsightSummary>> GetInsightTasksAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        List<ProjectTask> tasks = await dbContext.Tasks
            .Include(task => task.Links)
            .Include(task => task.Subtasks)
            .Where(task => task.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        return tasks.Select(task => ToInsightSummary(task, tasks)).ToList();
    }

    public async Task<bool> ReorderBacklogAsync(Guid projectId, IReadOnlyList<Guid> orderedTaskIds, CancellationToken cancellationToken = default)
    {
        List<ProjectTask> tasks = await dbContext.Tasks
            .Where(task => task.ProjectId == projectId && orderedTaskIds.Contains(task.Id))
            .ToListAsync(cancellationToken);

        if (tasks.Count != orderedTaskIds.Count)
        {
            return false;
        }

        var tasksById = tasks.ToDictionary(task => task.Id);
        for (int i = 0; i < orderedTaskIds.Count; i++)
        {
            tasksById[orderedTaskIds[i]].Reorder((i + 1) * 1024m);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Links are stored unidirectionally (see TaskLink): a "Blocks" link on the predecessor and a
    // "BlockedBy" link on the dependent are not auto-mirrored, so both directions must be reconciled
    // here to recover the full predecessor set for a task.
    private static ScheduleTaskSummary ToSummary(ProjectTask task, IReadOnlyList<ProjectTask> allProjectTasks)
    {
        var predecessorIds = new HashSet<Guid>(
            task.Links.Where(link => link.Type == TaskLinkType.BlockedBy).Select(link => link.LinkedTaskId));

        foreach (ProjectTask other in allProjectTasks)
        {
            if (other.Links.Any(link => link.Type == TaskLinkType.Blocks && link.LinkedTaskId == task.Id))
            {
                predecessorIds.Add(other.Id);
            }
        }

        return new ScheduleTaskSummary(
            task.Id,
            task.ProjectId,
            task.Key,
            task.Title,
            task.Status == ProjectTaskStatus.Done,
            task.Points,
            task.DueDate,
            predecessorIds.ToList(),
            task.AssigneeId);
    }

    private static TaskInsightSummary ToInsightSummary(ProjectTask task, IReadOnlyList<ProjectTask> allProjectTasks)
    {
        var predecessorIds = new HashSet<Guid>(
            task.Links.Where(link => link.Type == TaskLinkType.BlockedBy).Select(link => link.LinkedTaskId));
        int blocksCount = 0;

        foreach (ProjectTask other in allProjectTasks)
        {
            if (other.Links.Any(link => link.Type == TaskLinkType.Blocks && link.LinkedTaskId == task.Id))
            {
                predecessorIds.Add(other.Id);
            }

            if (task.Links.Any(link => link.Type == TaskLinkType.Blocks && link.LinkedTaskId == other.Id) ||
                other.Links.Any(link => link.Type == TaskLinkType.BlockedBy && link.LinkedTaskId == task.Id))
            {
                blocksCount++;
            }
        }

        return new TaskInsightSummary(
            task.Id,
            task.ProjectId,
            task.Key,
            task.Title,
            task.Status.ToString(),
            task.Priority.ToString(),
            task.Points,
            task.BusinessValue,
            task.DueDate,
            task.AssigneeId,
            task.SprintId,
            task.Rank,
            task.Subtasks.Count,
            task.Subtasks.Count(subtask => subtask.IsDone),
            predecessorIds.ToList(),
            blocksCount);
    }
}

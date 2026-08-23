using Microsoft.EntityFrameworkCore;
using PlanWise.Common.Infrastructure.Outbox;
using PlanWise.Modules.Delivery.Application.Abstractions.Data;
using PlanWise.Modules.Delivery.Domain.Activity;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Domain.Tasks;

namespace PlanWise.Modules.Delivery.Infrastructure.Database;

public sealed class DeliveryDbContext(DbContextOptions<DeliveryDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<Sprint> Sprints { get; set; }
    internal DbSet<ProjectTask> Tasks { get; set; }
    internal DbSet<ProjectTaskSequence> TaskSequences { get; set; }
    internal DbSet<ActivityLogEntry> ActivityLogEntries { get; set; }
    internal DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Delivery);

        modelBuilder.Entity<Sprint>(builder =>
        {
            builder.HasKey(sprint => sprint.Id);
            builder.Property(sprint => sprint.Name).HasMaxLength(200).IsRequired();
            builder.Property(sprint => sprint.Goal).HasMaxLength(1000);
            builder.Property(sprint => sprint.State).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.HasIndex(sprint => sprint.ProjectId);
        });

        modelBuilder.Entity<ProjectTask>(builder =>
        {
            builder.HasKey(task => task.Id);
            builder.Property(task => task.Key).HasMaxLength(20).IsRequired();
            builder.Property(task => task.Title).HasMaxLength(300).IsRequired();
            builder.Property(task => task.Description).HasMaxLength(5000);
            builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(task => task.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(task => task.Rank).HasPrecision(18, 4);
            builder.HasIndex(task => task.ProjectId);
            builder.HasIndex(task => new { task.ProjectId, task.Key }).IsUnique();
            builder.HasMany(task => task.Subtasks).WithOne().HasForeignKey(subtask => subtask.TaskId);
            builder.HasMany(task => task.Comments).WithOne().HasForeignKey(comment => comment.TaskId);
            builder.HasMany(task => task.Links).WithOne().HasForeignKey(link => link.TaskId);
            builder.HasMany(task => task.Labels).WithOne().HasForeignKey(label => label.TaskId);
        });

        modelBuilder.Entity<Subtask>(builder =>
        {
            builder.HasKey(subtask => subtask.Id);
            builder.Property(subtask => subtask.Title).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<TaskComment>(builder =>
        {
            builder.HasKey(comment => comment.Id);
            builder.Property(comment => comment.Body).HasMaxLength(5000).IsRequired();
        });

        modelBuilder.Entity<TaskLink>(builder =>
        {
            builder.HasKey(link => link.Id);
            builder.Property(link => link.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<TaskLabel>(builder =>
        {
            builder.HasKey(label => label.Id);
            builder.HasIndex(label => new { label.TaskId, label.LabelId }).IsUnique();
        });

        modelBuilder.Entity<ProjectTaskSequence>(builder =>
        {
            builder.HasKey(sequence => sequence.ProjectId);
        });

        modelBuilder.Entity<ActivityLogEntry>(builder =>
        {
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.Description).HasMaxLength(500).IsRequired();
            builder.HasIndex(entry => new { entry.ProjectId, entry.OccurredAtUtc });
        });

        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}

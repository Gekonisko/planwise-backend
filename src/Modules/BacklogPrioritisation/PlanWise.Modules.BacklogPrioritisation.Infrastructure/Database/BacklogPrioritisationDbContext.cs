using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.BacklogPrioritisation.Application.Abstractions.Data;
using PlanWise.Modules.BacklogPrioritisation.Domain.Priorities;

namespace PlanWise.Modules.BacklogPrioritisation.Infrastructure.Database;

public sealed class BacklogPrioritisationDbContext(DbContextOptions<BacklogPrioritisationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<PriorityRun> PriorityRuns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.BacklogPrioritisation);

        modelBuilder.Entity<PriorityRun>(builder =>
        {
            builder.HasKey(run => run.Id);
            builder.Property(run => run.ModelVersion).HasMaxLength(100).IsRequired();
            builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(run => run.DismissedReason).HasMaxLength(500);
            builder.HasIndex(run => run.ProjectId);
            builder.HasMany(run => run.Items).WithOne().HasForeignKey(item => item.RunId);
        });

        modelBuilder.Entity<PriorityItem>(builder =>
        {
            builder.HasKey(item => item.Id);
            builder.Property(item => item.TaskKey).HasMaxLength(20).IsRequired();
            builder.Property(item => item.Reason).HasMaxLength(500).IsRequired();
        });
    }
}

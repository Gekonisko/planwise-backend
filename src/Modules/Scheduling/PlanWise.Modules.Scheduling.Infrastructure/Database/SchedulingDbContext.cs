using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.Scheduling.Application.Abstractions.Data;
using PlanWise.Modules.Scheduling.Domain.Milestones;
using PlanWise.Modules.Scheduling.Domain.Optimisation;
using PlanWise.Modules.Scheduling.Domain.Schedule;

namespace PlanWise.Modules.Scheduling.Infrastructure.Database;

public sealed class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<Milestone> Milestones { get; set; }
    internal DbSet<ScheduleItem> ScheduleItems { get; set; }
    internal DbSet<ScheduleProposal> ScheduleProposals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Scheduling);

        modelBuilder.Entity<Milestone>(builder =>
        {
            builder.HasKey(milestone => milestone.Id);
            builder.Property(milestone => milestone.Name).HasMaxLength(200).IsRequired();
            builder.HasIndex(milestone => milestone.ProjectId);
        });

        modelBuilder.Entity<ScheduleItem>(builder =>
        {
            builder.HasKey(item => item.Id);
            builder.HasIndex(item => item.ProjectId);
        });

        modelBuilder.Entity<ScheduleProposal>(builder =>
        {
            builder.HasKey(proposal => proposal.Id);
            builder.Property(proposal => proposal.ModelName).HasMaxLength(100).IsRequired();
            builder.Property(proposal => proposal.Objective).HasMaxLength(500).IsRequired();
            builder.Property(proposal => proposal.ExpectedGain).HasMaxLength(500).IsRequired();
            builder.Property(proposal => proposal.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.HasIndex(proposal => proposal.ProjectId);
            builder.HasMany(proposal => proposal.Assignments).WithOne().HasForeignKey(assignment => assignment.ProposalId);
        });

        modelBuilder.Entity<ProposedAssignment>(builder =>
        {
            builder.HasKey(assignment => assignment.Id);
            builder.Property(assignment => assignment.TaskKey).HasMaxLength(20).IsRequired();
            builder.Property(assignment => assignment.ProposedAssigneeEmail).HasMaxLength(320).IsRequired();
        });
    }
}

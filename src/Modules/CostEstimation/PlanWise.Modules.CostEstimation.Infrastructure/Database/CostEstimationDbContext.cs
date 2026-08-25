using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.CostEstimation.Application.Abstractions.Data;
using PlanWise.Modules.CostEstimation.Domain.Budget;
using PlanWise.Modules.CostEstimation.Domain.Estimates;

namespace PlanWise.Modules.CostEstimation.Infrastructure.Database;

public sealed class CostEstimationDbContext(DbContextOptions<CostEstimationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<ProjectBudget> Budgets { get; set; }
    internal DbSet<CostEstimateRun> CostEstimateRuns { get; set; }
    internal DbSet<AppliedReduction> AppliedReductions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.CostEstimation);

        modelBuilder.Entity<ProjectBudget>(builder =>
        {
            builder.HasKey(budget => budget.Id);
            builder.Property(budget => budget.Currency).HasMaxLength(3).IsRequired();
            builder.Property(budget => budget.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CostEstimateRun>(builder =>
        {
            builder.HasKey(run => run.Id);
            builder.Property(run => run.ModelName).HasMaxLength(100).IsRequired();
            builder.Property(run => run.InputHash).HasMaxLength(64).IsRequired();
            builder.Property(run => run.Currency).HasMaxLength(3).IsRequired();
            builder.Property(run => run.ResultJson).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(run => run.ProjectId);
        });

        modelBuilder.Entity<AppliedReduction>(builder =>
        {
            builder.HasKey(applied => applied.Id);
            builder.HasIndex(applied => new { applied.RunId, applied.ReductionId }).IsUnique();
        });
    }
}

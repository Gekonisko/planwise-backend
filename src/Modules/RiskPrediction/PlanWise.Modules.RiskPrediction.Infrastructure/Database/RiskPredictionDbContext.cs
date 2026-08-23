using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.RiskPrediction.Application.Abstractions.Data;
using PlanWise.Modules.RiskPrediction.Domain.Risks;

namespace PlanWise.Modules.RiskPrediction.Infrastructure.Database;

public sealed class RiskPredictionDbContext(DbContextOptions<RiskPredictionDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<RiskAssessmentRun> RiskAssessmentRuns { get; set; }
    internal DbSet<TaskRiskAssessment> TaskRiskAssessments { get; set; }
    internal DbSet<SprintForecast> SprintForecasts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.RiskPrediction);

        modelBuilder.Entity<RiskAssessmentRun>(builder =>
        {
            builder.HasKey(run => run.Id);
            builder.Property(run => run.ModelVersion).HasMaxLength(100).IsRequired();
            builder.HasIndex(run => run.ProjectId);
        });

        modelBuilder.Entity<TaskRiskAssessment>(builder =>
        {
            builder.HasKey(assessment => assessment.Id);
            builder.Property(assessment => assessment.TaskKey).HasMaxLength(20).IsRequired();
            builder.Property(assessment => assessment.Reason).HasMaxLength(500).IsRequired();
            builder.Property(assessment => assessment.FeatureContributionsJson).HasColumnType("jsonb").IsRequired();
            builder.Property(assessment => assessment.DismissedReason).HasMaxLength(500);
            builder.HasIndex(assessment => assessment.RunId);
            builder.HasIndex(assessment => assessment.TaskId);
            builder.HasIndex(assessment => assessment.ProjectId);
        });

        modelBuilder.Entity<SprintForecast>(builder =>
        {
            builder.HasKey(forecast => forecast.Id);
            builder.HasIndex(forecast => forecast.RunId);
            builder.HasIndex(forecast => forecast.SprintId);
            builder.HasIndex(forecast => forecast.ProjectId);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PlanWise.Common.Infrastructure.Jobs;

public sealed class AsyncJobConfiguration : IEntityTypeConfiguration<AsyncJob>
{
    public void Configure(EntityTypeBuilder<AsyncJob> builder)
    {
        builder.ToTable("async_jobs");

        builder.HasKey(job => job.Id);

        builder.Property(job => job.JobType).HasMaxLength(100).IsRequired();
        builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(job => job.Error).HasMaxLength(2000);

        builder.HasIndex(job => job.ProjectId);
        builder.HasIndex(job => job.Status);
    }
}

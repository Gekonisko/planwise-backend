using Microsoft.EntityFrameworkCore;
using PlanWise.Common.Infrastructure.Jobs;

namespace PlanWise.Common.Infrastructure.Database;

public sealed class CommonDbContext(DbContextOptions<CommonDbContext> options) : DbContext(options)
{
    internal DbSet<AsyncJob> AsyncJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Common);

        modelBuilder.ApplyConfiguration(new AsyncJobConfiguration());
    }
}

using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.WorkspaceManagement.Application.Abstractions.Data;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;

namespace PlanWise.Modules.WorkspaceManagement.Infrastructure.Database;

public sealed class WorkspaceManagementDbContext(DbContextOptions<WorkspaceManagementDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<Project> Projects { get; set; }
    internal DbSet<ProjectMember> ProjectMembers { get; set; }
    internal DbSet<ProjectLabel> ProjectLabels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.WorkspaceManagement);

        modelBuilder.Entity<Project>(builder =>
        {
            builder.HasKey(project => project.Id);
            builder.Property(project => project.Name).HasMaxLength(200).IsRequired();
            builder.Property(project => project.KeyPrefix).HasMaxLength(10).IsRequired();
            builder.Property(project => project.Process).HasMaxLength(20).IsRequired();
            builder.Property(project => project.ClientName).HasMaxLength(200);
            builder.Property(project => project.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.HasIndex(project => project.KeyPrefix).IsUnique();
            builder.HasMany(project => project.Members).WithOne().HasForeignKey(member => member.ProjectId);
            builder.HasMany(project => project.Labels).WithOne().HasForeignKey(label => label.ProjectId);
        });

        modelBuilder.Entity<ProjectMember>(builder =>
        {
            builder.HasKey(member => member.Id);
            builder.Property(member => member.Email).HasMaxLength(300).IsRequired();
            builder.Property(member => member.Role).HasMaxLength(50).IsRequired();
            builder.Property(member => member.Capacity).HasPrecision(10, 2);
            builder.Property(member => member.HourlyRate).HasPrecision(12, 2);
            builder.HasIndex(member => new { member.ProjectId, member.UserId }).IsUnique();
        });

        modelBuilder.Entity<ProjectLabel>(builder =>
        {
            builder.HasKey(label => label.Id);
            builder.Property(label => label.Name).HasMaxLength(100).IsRequired();
            builder.Property(label => label.Color).HasMaxLength(20).IsRequired();
            builder.HasIndex(label => new { label.ProjectId, label.Name }).IsUnique();
        });
    }
}
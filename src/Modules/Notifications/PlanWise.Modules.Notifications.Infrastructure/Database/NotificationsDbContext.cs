using Microsoft.EntityFrameworkCore;
using PlanWise.Modules.Notifications.Application.Abstractions.Data;
using PlanWise.Modules.Notifications.Domain.Notifications;

namespace PlanWise.Modules.Notifications.Infrastructure.Database;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options), IUnitOfWork
{
    internal DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schemas.Notifications);

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.HasKey(notification => notification.Id);
            builder.Property(notification => notification.Type).HasMaxLength(50).IsRequired();
            builder.Property(notification => notification.Message).HasMaxLength(500).IsRequired();
            builder.Property(notification => notification.Link).HasMaxLength(500);
            builder.HasIndex(notification => new { notification.UserId, notification.CreatedAtUtc });
        });
    }
}

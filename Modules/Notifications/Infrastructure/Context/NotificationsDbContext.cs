using Microsoft.EntityFrameworkCore;
using Modules.Notifications.Domain.Entities;

namespace Modules.Notifications.Infrastructure.Context
{
    public class NotificationsDbContext : DbContext
    {
        public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
            : base(options)
        {
        }

        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<NotificationItem> Notifications => Set<NotificationItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.ToTable("ActivityLogs");
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => a.WorkspaceId);
                entity.HasIndex(a => a.ProjectId);
                entity.HasIndex(a => a.TaskId);
            });

            modelBuilder.Entity<NotificationItem>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(n => n.Id);
                entity.HasIndex(n => new { n.UserId, n.IsRead });
            });
        }
    }
}

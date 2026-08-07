using Microsoft.EntityFrameworkCore;
using Modules.TimeTracking.Domain.Entities;

namespace Modules.TimeTracking.Infrastructure.Context
{
    public class TimeTrackingDbContext : DbContext
    {
        public TimeTrackingDbContext(DbContextOptions<TimeTrackingDbContext> options) : base(options)
        {
        }

        public DbSet<TimeLog> TimeLogs => Set<TimeLog>();
        public DbSet<TaskLookup> TaskLookups => Set<TaskLookup>();
        public DbSet<ProjectLookup> ProjectLookups => Set<ProjectLookup>();
        public DbSet<UserLookup> UserLookups => Set<UserLookup>();
        public DbSet<WorkspaceLookup> WorkspaceLookups => Set<WorkspaceLookup>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TimeLog>(entity =>
            {
                entity.ToTable("TimeLogs");
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => t.TaskId);
                // BR-23-01: only one active (EndedAt == null) timer per user at a time.
                entity.HasIndex(t => t.UserId).IsUnique().HasFilter("[EndedAt] IS NULL");
            });

            modelBuilder.Entity<TaskLookup>(entity =>
            {
                entity.ToTable("Tasks", t => t.ExcludeFromMigrations());
                entity.HasKey(t => t.Id);
            });

            modelBuilder.Entity<ProjectLookup>(entity =>
            {
                entity.ToTable("Projects", t => t.ExcludeFromMigrations());
                entity.HasKey(p => p.Id);
            });

            modelBuilder.Entity<UserLookup>(entity =>
            {
                entity.ToTable("Users", t => t.ExcludeFromMigrations());
                entity.HasKey(u => u.Id);
            });

            modelBuilder.Entity<WorkspaceLookup>(entity =>
            {
                entity.ToTable("Workspaces", t => t.ExcludeFromMigrations());
                entity.HasKey(w => w.Id);
            });
        }
    }
}

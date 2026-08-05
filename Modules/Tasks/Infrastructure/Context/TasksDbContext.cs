using Microsoft.EntityFrameworkCore;
using Modules.Tasks.Domain.Entities;

namespace Modules.Tasks.Infrastructure.Context
{
    public class TasksDbContext : DbContext
    {
        public TasksDbContext(DbContextOptions<TasksDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
        public DbSet<TaskAssignee> TaskAssignees => Set<TaskAssignee>();
        public DbSet<TaskWatcher> TaskWatchers => Set<TaskWatcher>();
        public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
        public DbSet<RecurringTaskRule> RecurringTaskRules => Set<RecurringTaskRule>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskEntity>(entity =>
            {
                entity.ToTable("Tasks");
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => t.ProjectId);
                entity.HasIndex(t => t.PrimaryAssigneeUserId);
                entity.HasIndex(t => t.ParentTaskId);
                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => new { t.ProjectId, t.Status, t.SortOrder });
                entity.Property(t => t.EstimatedHours).HasPrecision(18, 2);
                entity.Property(t => t.ActualHours).HasPrecision(18, 2);

                entity.HasOne(t => t.ParentTask)
                      .WithMany(p => p.Subtasks)
                      .HasForeignKey(t => t.ParentTaskId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TaskAssignee>(entity =>
            {
                entity.ToTable("TaskAssignees");
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => new { a.TaskId, a.UserId }).IsUnique();
                entity.HasOne(a => a.Task)
                      .WithMany(t => t.Assignees)
                      .HasForeignKey(a => a.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TaskWatcher>(entity =>
            {
                entity.ToTable("TaskWatchers");
                entity.HasKey(w => w.Id);
                entity.HasIndex(w => new { w.TaskId, w.UserId }).IsUnique();
                entity.HasOne(w => w.Task)
                      .WithMany(t => t.Watchers)
                      .HasForeignKey(w => w.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ChecklistItem>(entity =>
            {
                entity.ToTable("ChecklistItems");
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.TaskId);
                entity.HasOne(c => c.Task)
                      .WithMany()
                      .HasForeignKey(c => c.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RecurringTaskRule>(entity =>
            {
                entity.ToTable("RecurringTaskRules");
                entity.HasKey(r => r.Id);
                entity.HasIndex(r => r.TaskId).IsUnique();
                entity.HasOne(r => r.Task)
                      .WithMany()
                      .HasForeignKey(r => r.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

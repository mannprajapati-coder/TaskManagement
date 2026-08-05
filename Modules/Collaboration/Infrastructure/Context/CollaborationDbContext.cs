using Microsoft.EntityFrameworkCore;
using Modules.Collaboration.Domain.Entities;

namespace Modules.Collaboration.Infrastructure.Context
{
    public class CollaborationDbContext : DbContext
    {
        public CollaborationDbContext(DbContextOptions<CollaborationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskComment> TaskComments => Set<TaskComment>();
        public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.ToTable("TaskComments");
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => c.TaskId);
            });

            modelBuilder.Entity<TaskAttachment>(entity =>
            {
                entity.ToTable("TaskAttachments");
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => a.TaskId);
            });
        }
    }
}

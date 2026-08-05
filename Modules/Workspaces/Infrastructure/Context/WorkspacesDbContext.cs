using Microsoft.EntityFrameworkCore;
using Modules.Workspaces.Domain.Entities;

namespace Modules.Workspaces.Infrastructure.Context
{
    public class WorkspacesDbContext : DbContext
    {
        public WorkspacesDbContext(DbContextOptions<WorkspacesDbContext> options)
            : base(options)
        {
        }

        public DbSet<Workspace> Workspaces => Set<Workspace>();
        public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
        public DbSet<WorkspaceInvite> WorkspaceInvites => Set<WorkspaceInvite>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Workspace>(entity =>
            {
                entity.ToTable("Workspaces");
                entity.HasKey(w => w.Id);
                entity.HasIndex(w => w.OwnerUserId);
            });

            modelBuilder.Entity<WorkspaceMember>(entity =>
            {
                entity.ToTable("WorkspaceMembers");
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => new { m.WorkspaceId, m.UserId }).IsUnique();
                entity.HasOne(m => m.Workspace)
                      .WithMany(w => w.Members)
                      .HasForeignKey(m => m.WorkspaceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WorkspaceInvite>(entity =>
            {
                entity.ToTable("WorkspaceInvites");
                entity.HasKey(i => i.Id);
                entity.HasIndex(i => i.TokenHash).IsUnique();
                entity.HasOne(i => i.Workspace)
                      .WithMany(w => w.Invites)
                      .HasForeignKey(i => i.WorkspaceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

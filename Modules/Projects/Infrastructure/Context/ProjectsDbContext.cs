using Microsoft.EntityFrameworkCore;
using Modules.Projects.Domain.Entities;

namespace Modules.Projects.Infrastructure.Context
{
    public class ProjectsDbContext : DbContext
    {
        public ProjectsDbContext(DbContextOptions<ProjectsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectFavorite> ProjectFavorites => Set<ProjectFavorite>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<ProjectJoinRequest> ProjectJoinRequests => Set<ProjectJoinRequest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>(entity =>
            {
                entity.ToTable("Projects");
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.WorkspaceId);
            });

            modelBuilder.Entity<ProjectFavorite>(entity =>
            {
                entity.ToTable("ProjectFavorites");
                entity.HasKey(f => f.Id);
                entity.HasIndex(f => new { f.ProjectId, f.UserId }).IsUnique();
                entity.HasOne(f => f.Project)
                      .WithMany(p => p.Favorites)
                      .HasForeignKey(f => f.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProjectMember>(entity =>
            {
                entity.ToTable("ProjectMembers");
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => new { m.ProjectId, m.UserId }).IsUnique();
                entity.HasOne(m => m.Project)
                      .WithMany(p => p.Members)
                      .HasForeignKey(m => m.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProjectJoinRequest>(entity =>
            {
                entity.ToTable("ProjectJoinRequests");
                entity.HasKey(r => r.Id);
                entity.HasIndex(r => new { r.ProjectId, r.RequestingUserId });
                entity.HasOne(r => r.Project)
                      .WithMany(p => p.JoinRequests)
                      .HasForeignKey(r => r.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}

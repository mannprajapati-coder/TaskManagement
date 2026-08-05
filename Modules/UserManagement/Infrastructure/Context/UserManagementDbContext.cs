using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Modules.UserManagement.Domain.Entities;

namespace Modules.UserManagement.Infrastructure.Context
{
    public class UserManagementDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
        public DbSet<ActiveSession> ActiveSessions => Set<ActiveSession>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.GoogleSubjectId).IsUnique().HasFilter("[GoogleSubjectId] IS NOT NULL");
            });

            builder.Entity<UserPreference>(entity =>
            {
                entity.ToTable("UserPreferences");
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.UserId).IsUnique();
            });

            builder.Entity<ActiveSession>(entity =>
            {
                entity.ToTable("ActiveSessions");
                entity.HasKey(s => s.Id);
                entity.HasIndex(s => s.UserId);
            });
        }
    }
}

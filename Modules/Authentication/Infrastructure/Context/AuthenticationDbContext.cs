using Microsoft.EntityFrameworkCore;
using Modules.Authentication.Domain.Entities;

namespace Modules.Authentication.Infrastructure.Context
{
    public class AuthenticationDbContext : DbContext
    {
        public AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
        public DbSet<MfaSecret> MfaSecrets => Set<MfaSecret>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(r => r.Id);
                entity.HasIndex(r => r.TokenHash).IsUnique();
                entity.HasIndex(r => r.UserId);
                entity.HasIndex(r => r.FamilyId);
            });

            modelBuilder.Entity<EmailVerificationToken>(entity =>
            {
                entity.ToTable("EmailVerificationTokens");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TokenHash);
                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("PasswordResetTokens");
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => p.TokenHash);
                entity.HasIndex(p => p.UserId);
            });

            modelBuilder.Entity<MfaSecret>(entity =>
            {
                entity.ToTable("MfaSecrets");
                entity.HasKey(m => m.Id);
                entity.HasIndex(m => m.UserId).IsUnique();
            });
        }
    }
}

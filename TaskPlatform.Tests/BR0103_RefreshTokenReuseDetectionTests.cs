using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Authentication.Application.Services;
using Modules.Authentication.Domain.Entities;
using Modules.Authentication.Infrastructure.Context;
using Modules.UserManagement.Domain.Entities;
using Modules.UserManagement.Infrastructure.Context;
using Moq;
using TaskPlatform.Shared.Exceptions;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR0103_RefreshTokenReuseDetectionTests
    {
        [Fact]
        public async Task RefreshTokenAsync_ReusingAlreadyRevokedToken_ShouldRevokeWholeFamilyAndThrow()
        {
            // Arrange
            var optionsAuth = new DbContextOptionsBuilder<AuthenticationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var optionsUser = new DbContextOptionsBuilder<UserManagementDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var authDb = new AuthenticationDbContext(optionsAuth);
            using var userDb = new UserManagementDbContext(optionsUser);

            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Email = "reuse@example.com", FullName = "Reuse User" };
            userDb.Users.Add(user);
            await userDb.SaveChangesAsync();

            var familyId = Guid.NewGuid();
            var rawToken = "already_used_refresh_token";
            var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            // Add already rotated/revoked token
            var revokedToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                FamilyId = familyId,
                RevokedAt = DateTime.UtcNow.AddMinutes(-5) // Revoked 5 mins ago!
            };

            // Add active child token in same family
            var activeChildToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = "child_hash",
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                FamilyId = familyId,
                RevokedAt = null
            };

            authDb.RefreshTokens.AddRange(revokedToken, activeChildToken);
            await authDb.SaveChangesAsync();

            var jwtMock = new Mock<IJwtTokenGenerator>();
            var emailMock = new Mock<IEmailSender>();
            var authService = new AuthService(authDb, userDb, jwtMock.Object, emailMock.Object);

            // Act & Assert
            Func<Task> act = async () => await authService.RefreshTokenAsync(rawToken);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Token reuse detected — all sessions revoked.*");

            // Verify entire family revoked in DB
            var familyTokens = await authDb.RefreshTokens.Where(r => r.FamilyId == familyId).ToListAsync();
            familyTokens.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
        }
    }
}

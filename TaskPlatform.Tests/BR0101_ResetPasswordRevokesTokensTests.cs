using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

using Modules.Authentication.Application.Services;
using Modules.Authentication.Domain.Entities;
using Modules.Authentication.Infrastructure.Context;
using Modules.UserManagement.Domain.Entities;
using Modules.UserManagement.Infrastructure.Context;
using Moq;
using TaskPlatform.Shared.ViewModels.Auth;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR0101_ResetPasswordRevokesTokensTests
    {
        [Fact]
        public async Task ResetPasswordAsync_ShouldRevokeAllRefreshTokensForUser()
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
            var user = new User { Id = userId, Email = "test@example.com", FullName = "Test User", IsEmailVerified = true };
            userDb.Users.Add(user);
            await userDb.SaveChangesAsync();

            // Add active refresh tokens for user
            var rt1 = RefreshToken.CreateNew(userId, "hash1", TimeSpan.FromDays(30));
            var rt2 = RefreshToken.CreateNew(userId, "hash2", TimeSpan.FromDays(30));
            authDb.RefreshTokens.AddRange(rt1, rt2);

            // Add Reset token
            var tokenHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("valid_reset_token")));
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            authDb.PasswordResetTokens.Add(resetToken);
            await authDb.SaveChangesAsync();

            var jwtMock = new Mock<IJwtTokenGenerator>();
            var emailMock = new Mock<IEmailSender>();

            var authService = new AuthService(authDb, userDb, jwtMock.Object, emailMock.Object);

            var resetModel = new ResetPasswordRequestViewModel
            {
                UserId = userId.ToString(),
                Token = "valid_reset_token",
                NewPassword = "NewSecurePassword123!",
                ConfirmPassword = "NewSecurePassword123!"
            };

            // Act
            var result = await authService.ResetPasswordAsync(resetModel);

            // Assert
            result.Should().BeTrue();
            var tokens = await authDb.RefreshTokens.ToListAsync();
            tokens.Should().AllSatisfy(t => t.RevokedAt.Should().NotBeNull());
        }
    }
}

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Authentication.Application.Services;
using Modules.Authentication.Infrastructure.Context;
using Modules.UserManagement.Domain.Entities;
using Modules.UserManagement.Infrastructure.Context;
using Moq;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.Auth;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR0102_EmailVerificationRequiredTests
    {
        [Fact]
        public async Task LoginAsync_UnverifiedEmail_ShouldThrowDomainException()
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

            var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();
            var user = new User { Id = Guid.NewGuid(), Email = "unverified@example.com", IsEmailVerified = false };
            user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");

            userDb.Users.Add(user);
            await userDb.SaveChangesAsync();

            var jwtMock = new Mock<IJwtTokenGenerator>();
            var emailMock = new Mock<IEmailSender>();
            var authService = new AuthService(authDb, userDb, jwtMock.Object, emailMock.Object);

            var loginModel = new LoginRequestViewModel
            {
                Email = "unverified@example.com",
                Password = "Password123!"
            };

            // Act & Assert
            Func<Task> act = async () => await authService.LoginAsync(loginModel);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*verify your email address*");
        }
    }
}

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Workspaces.Application.Services;
using Modules.Workspaces.Domain.Entities;
using Modules.Workspaces.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR0401_WorkspaceInviteTokenExpiryTests
    {
        [Fact]
        public async Task JoinViaInviteAsync_ExpiredToken_ShouldThrowDomainException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<WorkspacesDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new WorkspacesDbContext(options);

            var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Test WS", OwnerUserId = Guid.NewGuid() };
            dbContext.Workspaces.Add(workspace);

            var rawToken = "expired_invite_token";
            var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            var expiredInvite = new WorkspaceInvite
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspace.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // Expired 10 mins ago!
                MaxUses = 10,
                UseCount = 0
            };
            dbContext.WorkspaceInvites.Add(expiredInvite);
            await dbContext.SaveChangesAsync();

            var workspaceService = new WorkspaceService(dbContext);
            var joiningUserId = Guid.NewGuid();

            // Act & Assert
            Func<Task> act = async () => await workspaceService.JoinViaInviteAsync(joiningUserId, rawToken);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*expired*");
        }

        [Fact]
        public async Task JoinViaInviteAsync_MaxUsesReached_ShouldThrowDomainException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<WorkspacesDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new WorkspacesDbContext(options);

            var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Test WS 2", OwnerUserId = Guid.NewGuid() };
            dbContext.Workspaces.Add(workspace);

            var rawToken = "exhausted_invite_token";
            var tokenHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

            var exhaustedInvite = new WorkspaceInvite
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspace.Id,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                MaxUses = 5,
                UseCount = 5 // Maxed out!
            };
            dbContext.WorkspaceInvites.Add(exhaustedInvite);
            await dbContext.SaveChangesAsync();

            var workspaceService = new WorkspaceService(dbContext);
            var joiningUserId = Guid.NewGuid();

            // Act & Assert
            Func<Task> act = async () => await workspaceService.JoinViaInviteAsync(joiningUserId, rawToken);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*limit has been reached*");
        }
    }
}

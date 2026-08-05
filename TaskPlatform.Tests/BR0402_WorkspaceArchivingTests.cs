using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Workspaces.Application.Services;
using Modules.Workspaces.Domain.Entities;
using Modules.Workspaces.Infrastructure.Context;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR0402_WorkspaceArchivingTests
    {
        [Fact]
        public async Task ArchiveWorkspaceAsync_ShouldMarkIsArchivedTrueAndExcludeFromGetMyWorkspaces()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<WorkspacesDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new WorkspacesDbContext(options);
            var ownerId = Guid.NewGuid();

            var workspace = new Workspace
            {
                Id = Guid.NewGuid(),
                Name = "Active Workspace",
                OwnerUserId = ownerId,
                IsArchived = false
            };
            var member = new WorkspaceMember { Id = Guid.NewGuid(), WorkspaceId = workspace.Id, UserId = ownerId, Role = "Owner" };

            dbContext.Workspaces.Add(workspace);
            dbContext.WorkspaceMembers.Add(member);
            await dbContext.SaveChangesAsync();

            var workspaceService = new WorkspaceService(dbContext);

            // Act
            var archiveResult = await workspaceService.ArchiveWorkspaceAsync(ownerId, workspace.Id);
            var userWorkspaces = await workspaceService.GetUserWorkspacesAsync(ownerId);

            // Assert
            archiveResult.Should().BeTrue();
            userWorkspaces.Should().BeEmpty(); // Archived workspace excluded from default active list
        }
    }
}

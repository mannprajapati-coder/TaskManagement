using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Notifications.Application.Services;
using Modules.Notifications.Infrastructure.Context;
using TaskPlatform.Shared.ViewModels.Notification;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR1801_ActivityLogRecordingTests
    {
        [Fact]
        public async Task LogActivityAsync_ValidPayload_ShouldRecordActivityInAuditLog()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NotificationsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new NotificationsDbContext(options);
            var service = new NotificationService(dbContext);
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();

            var model = new CreateActivityLogRequestViewModel
            {
                WorkspaceId = workspaceId,
                Action = "ProjectCreated",
                Details = "Created project 'Mobile App 2026'"
            };

            // Act
            var result = await service.LogActivityAsync(userId, model);

            // Assert
            result.Should().BeTrue();

            var logs = await service.GetWorkspaceActivityAsync(workspaceId);
            logs.Should().ContainSingle(l => l.Action == "ProjectCreated");
        }
    }
}

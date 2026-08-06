using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Notifications.Domain.IServices;
using Modules.Tasks.Application.Services;
using Modules.Tasks.Domain.Entities;
using Modules.Tasks.Infrastructure.Context;
using Moq;
using TaskPlatform.Shared.ViewModels.Notification;
using TaskPlatform.Shared.ViewModels.Task;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR2401_DashboardMetricsAggregationTests
    {
        [Fact]
        public async Task GetWorkspaceDashboardOverviewAsync_MultipleTasks_ShouldCalculateCompletionRateCorrectly()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var tasksService = TasksServiceTestFactory.Create(dbContext);
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            // BR-24-01: DashboardService now scopes tasks to the workspace via ProjectLookups
            // (the read-only projection of the Projects table), so the test must seed the
            // project/workspace relationship it depends on.
            dbContext.ProjectLookups.Add(new ProjectLookup { Id = projectId, WorkspaceId = workspaceId });
            await dbContext.SaveChangesAsync();

            var notificationService = new Mock<INotificationService>();
            notificationService
                .Setup(n => n.GetWorkspaceActivityAsync(It.IsAny<Guid>()))
                .ReturnsAsync(new List<ActivityLogViewModel>());

            var dashboardService = new DashboardService(dbContext, notificationService.Object);

            var task1 = await tasksService.CreateTaskAsync(userId, new CreateTaskRequestViewModel
            {
                ProjectId = projectId,
                Title = "Task One"
            });

            var task2 = await tasksService.CreateTaskAsync(userId, new CreateTaskRequestViewModel
            {
                ProjectId = projectId,
                Title = "Task Two"
            });

            // Mark task1 completed
            await tasksService.UpdateTaskStatusAsync(userId, new UpdateTaskStatusRequestViewModel
            {
                TaskId = task1.Id,
                Status = "Completed"
            });

            // Act
            var overview = await dashboardService.GetWorkspaceDashboardOverviewAsync(workspaceId, userId);

            // Assert
            overview.TotalTasks.Should().Be(2);
            overview.CompletedTasks.Should().Be(1);
            overview.PendingTasks.Should().Be(1);
            overview.CompletionRatePercentage.Should().Be(50.0);
        }
    }
}

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Tasks.Application.Services;
using Modules.Tasks.Infrastructure.Context;
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
            var tasksService = new TasksService(dbContext);
            var dashboardService = new DashboardService(dbContext);
            var userId = Guid.NewGuid();
            var workspaceId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

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

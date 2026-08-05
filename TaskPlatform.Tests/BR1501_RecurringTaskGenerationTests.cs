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
    public class BR1501_RecurringTaskGenerationTests
    {
        [Fact]
        public async Task ProcessDueRecurringTasks_DueRule_ShouldCreateNewRecurringTask()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = new TasksService(dbContext);
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();

            var baseTask = await service.CreateTaskAsync(userId, new CreateTaskRequestViewModel
            {
                ProjectId = projectId,
                Title = "Weekly Backup"
            });

            // Set recurring rule due immediately
            await service.SetRecurringTaskRuleAsync(userId, new SetRecurringTaskRuleRequestViewModel
            {
                TaskId = baseTask.Id,
                RecurrencePattern = "Daily",
                Interval = 1,
                StartRunDate = DateTime.UtcNow.AddMinutes(-5) // Past run date
            });

            // Act
            var createdCount = await service.ProcessDueRecurringTasksAsync();

            // Assert
            createdCount.Should().Be(1);

            var projectTasks = await service.GetProjectTasksAsync(projectId);
            projectTasks.Should().HaveCount(2); // Base task + newly generated recurring task
        }
    }
}

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Tasks.Application.Services;
using Modules.Tasks.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.Task;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR1001_TaskStatusAndHoursValidationTests
    {
        [Fact]
        public async Task UpdateTaskStatusAsync_ToCompleted_ShouldAutoSetCompletedAtTimestamp()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = new TasksService(dbContext);
            var userId = Guid.NewGuid();

            var createModel = new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Test Completion Task"
            };

            var createdTask = await service.CreateTaskAsync(userId, createModel);
            createdTask.CompletedAt.Should().BeNull();

            // Act - Move status to Completed
            var updateStatusModel = new UpdateTaskStatusRequestViewModel
            {
                TaskId = createdTask.Id,
                Status = "Completed"
            };
            var updatedTask = await service.UpdateTaskStatusAsync(userId, updateStatusModel);

            // Assert
            updatedTask.Status.Should().Be("Completed");
            updatedTask.CompletedAt.Should().NotBeNull();
            updatedTask.CompletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Act 2 - Move status back to InProgress
            var revertModel = new UpdateTaskStatusRequestViewModel
            {
                TaskId = createdTask.Id,
                Status = "InProgress"
            };
            var revertedTask = await service.UpdateTaskStatusAsync(userId, revertModel);

            // Assert 2
            revertedTask.Status.Should().Be("InProgress");
            revertedTask.CompletedAt.Should().BeNull(); // CompletedAt cleared when moving away from Completed
        }

        [Fact]
        public async Task UpdateTaskAsync_NegativeActualHours_ShouldThrowDomainException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = new TasksService(dbContext);
            var userId = Guid.NewGuid();

            var createModel = new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Task For Hours Test"
            };
            var createdTask = await service.CreateTaskAsync(userId, createModel);

            var updateModel = new UpdateTaskRequestViewModel
            {
                TaskId = createdTask.Id,
                Title = createdTask.Title,
                ActualHours = -5 // Negative hours!
            };

            // Act & Assert
            Func<Task> act = async () => await service.UpdateTaskAsync(userId, updateModel);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Actual hours must be non-negative*");
        }

        [Fact]
        public async Task CreateTaskAsync_DueDateBeforeStartDate_ShouldThrowDomainException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = new TasksService(dbContext);
            var userId = Guid.NewGuid();

            var createModel = new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Invalid Date Task",
                StartDate = DateTime.UtcNow.AddDays(5),
                DueDate = DateTime.UtcNow.AddDays(1) // Due date before start date!
            };

            // Act & Assert
            Func<Task> act = async () => await service.CreateTaskAsync(userId, createModel);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Due Date must be on or after Start Date*");
        }
    }
}

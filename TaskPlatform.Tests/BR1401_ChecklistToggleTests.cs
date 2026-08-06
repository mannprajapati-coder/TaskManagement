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
    public class BR1401_ChecklistToggleTests
    {
        [Fact]
        public async Task AddChecklistItem_AndToggle_ShouldUpdateItemCompletionState()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = TasksServiceTestFactory.Create(dbContext);
            var userId = Guid.NewGuid();

            var task = await service.CreateTaskAsync(userId, new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Task with Checklist"
            });

            // Act 1: Add Checklist Item
            var item = await service.AddChecklistItemAsync(userId, new AddChecklistItemRequestViewModel
            {
                TaskId = task.Id,
                Title = "Verify Database Schema"
            });

            // Assert 1
            item.Should().NotBeNull();
            item.IsCompleted.Should().BeFalse();

            // Act 2: Toggle Checklist Item
            var toggleResult = await service.ToggleChecklistItemAsync(userId, item.Id);

            // Assert 2
            toggleResult.Should().BeTrue();
            var items = await service.GetChecklistItemsAsync(task.Id);
            items.Should().ContainSingle(i => i.Id == item.Id && i.IsCompleted == true);
        }

        [Fact]
        public async Task UpdateTaskStatusAsync_CompletedWithIncompleteChecklistItem_ShouldThrowDomainException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = TasksServiceTestFactory.Create(dbContext);
            var userId = Guid.NewGuid();

            var task = await service.CreateTaskAsync(userId, new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Task with Checklist"
            });

            await service.AddChecklistItemAsync(userId, new AddChecklistItemRequestViewModel
            {
                TaskId = task.Id,
                Title = "Unchecked item"
            });

            // Act & Assert
            Func<Task> act = async () => await service.UpdateTaskStatusAsync(userId, new UpdateTaskStatusRequestViewModel
            {
                TaskId = task.Id,
                Status = "Completed"
            });

            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*checklist items remain incomplete*");
        }

        [Fact]
        public async Task UpdateTaskStatusAsync_CompletedWithAllChecklistItemsChecked_ShouldSucceed()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = TasksServiceTestFactory.Create(dbContext);
            var userId = Guid.NewGuid();

            var task = await service.CreateTaskAsync(userId, new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Task with Checklist"
            });

            var item = await service.AddChecklistItemAsync(userId, new AddChecklistItemRequestViewModel
            {
                TaskId = task.Id,
                Title = "Item to complete"
            });
            await service.ToggleChecklistItemAsync(userId, item.Id);

            // Act
            var updated = await service.UpdateTaskStatusAsync(userId, new UpdateTaskStatusRequestViewModel
            {
                TaskId = task.Id,
                Status = "Completed"
            });

            // Assert
            updated.Status.Should().Be("Completed");
        }
    }
}

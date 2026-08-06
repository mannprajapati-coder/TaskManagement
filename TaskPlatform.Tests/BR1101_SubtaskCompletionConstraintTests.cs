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
    public class BR1101_SubtaskCompletionConstraintTests
    {
        [Fact]
        public async Task UpdateTaskStatusAsync_ParentCompletedWithIncompleteSubtasks_ShouldThrowDomainException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = TasksServiceTestFactory.Create(dbContext);
            var userId = Guid.NewGuid();

            var parentTask = await service.CreateTaskAsync(userId, new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Parent Feature Task"
            });

            // Create an incomplete subtask
            var subtask = await service.CreateSubtaskAsync(userId, new CreateSubtaskRequestViewModel
            {
                ParentTaskId = parentTask.Id,
                ProjectId = parentTask.ProjectId,
                Title = "Incomplete Subtask Step"
            });

            // Act & Assert - Try to mark parent task as Completed while subtask is incomplete
            var updateStatusModel = new UpdateTaskStatusRequestViewModel
            {
                TaskId = parentTask.Id,
                Status = "Completed"
            };

            Func<Task> act = async () => await service.UpdateTaskStatusAsync(userId, updateStatusModel);
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*Cannot mark parent task as Completed while incomplete subtasks remain*");
        }
    }
}

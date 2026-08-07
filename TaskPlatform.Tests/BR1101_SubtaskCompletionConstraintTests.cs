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
    public class BR1101_SubtaskCompletionConstraintTests
    {
        [Fact]
        public async Task UpdateTaskStatusAsync_ParentCompletedWithIncompleteSubtasks_ShouldReturnFailureResult()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<TasksDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new TasksDbContext(options);
            var service = TasksServiceTestFactory.Create(dbContext);
            var userId = Guid.NewGuid();

            var parentTaskResponse = await service.CreateTaskAsync(userId, new CreateTaskRequestViewModel
            {
                ProjectId = Guid.NewGuid(),
                Title = "Parent Feature Task"
            });
            var parentTask = parentTaskResponse.Data!;

            // Create an incomplete subtask
            var subtaskResponse = await service.CreateSubtaskAsync(userId, new CreateSubtaskRequestViewModel
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

            var result = await service.UpdateTaskStatusAsync(userId, updateStatusModel);
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Cannot mark parent task as Completed while incomplete subtasks remain");
        }
    }
}

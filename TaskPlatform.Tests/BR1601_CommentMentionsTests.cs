using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Modules.Collaboration.Application.Services;
using Modules.Collaboration.Infrastructure.Context;
using TaskPlatform.Shared.ViewModels.Collaboration;
using Xunit;

namespace TaskPlatform.Tests
{
    public class BR1601_CommentMentionsTests
    {
        [Fact]
        public async Task AddTaskCommentAsync_WithMentions_ShouldSaveAndReturnMentionedUserIds()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<CollaborationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new CollaborationDbContext(options);
            var service = new CollaborationService(dbContext);
            var userId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var mentionedUser = Guid.NewGuid().ToString();

            var model = new AddCommentRequestViewModel
            {
                TaskId = taskId,
                CommentText = "Hey @Developer, please review this PR!",
                MentionedUserIds = new List<string> { mentionedUser }
            };

            // Act
            var comment = await service.AddTaskCommentAsync(userId, model);

            // Assert
            comment.Should().NotBeNull();
            comment.CommentText.Should().Contain("review this PR");
            comment.MentionedUserIds.Should().ContainSingle(m => m == mentionedUser);
        }
    }
}

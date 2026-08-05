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
    public class BR1901_NotificationDeliveryTests
    {
        [Fact]
        public async Task SendNotificationAsync_AndMarkRead_ShouldUpdateReadStatus()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<NotificationsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var dbContext = new NotificationsDbContext(options);
            var service = new NotificationService(dbContext);
            var userId = Guid.NewGuid();

            var sendModel = new SendNotificationRequestViewModel
            {
                UserId = userId,
                Title = "Task Assigned",
                Message = "You were assigned to Task #104."
            };

            // Act 1: Send Notification
            await service.SendNotificationAsync(sendModel);

            var unreadList = await service.GetUserNotificationsAsync(userId, unreadOnly: true);
            unreadList.Should().ContainSingle(n => n.Title == "Task Assigned");

            // Act 2: Mark as read
            var notificationId = unreadList[0].Id;
            await service.MarkAsReadAsync(userId, notificationId);

            // Assert 2
            var remainingUnread = await service.GetUserNotificationsAsync(userId, unreadOnly: true);
            remainingUnread.Should().BeEmpty();
        }
    }
}

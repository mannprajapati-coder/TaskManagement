using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Notification;

namespace Modules.Notifications.Domain.IServices
{
    public interface INotificationService
    {
        Task<List<ActivityLogViewModel>> GetWorkspaceActivityAsync(Guid workspaceId);
        Task<List<ActivityLogViewModel>> GetProjectActivityAsync(Guid projectId);
        Task<List<ActivityLogViewModel>> GetTaskActivityAsync(Guid taskId);
        Task<bool> LogActivityAsync(Guid actorUserId, CreateActivityLogRequestViewModel model);

        Task<List<NotificationItemViewModel>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false);
        Task<NotificationItemViewModel> SendNotificationAsync(SendNotificationRequestViewModel model);
        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
        Task<bool> MarkAllAsReadAsync(Guid userId);
    }
}

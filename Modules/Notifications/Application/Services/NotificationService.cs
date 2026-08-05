using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Notifications.Domain.Entities;
using Modules.Notifications.Domain.IServices;
using Modules.Notifications.Infrastructure.Context;
using TaskPlatform.Shared.ViewModels.Notification;

namespace Modules.Notifications.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NotificationsDbContext _dbContext;

        public NotificationService(NotificationsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ActivityLogViewModel>> GetWorkspaceActivityAsync(Guid workspaceId)
        {
            var logs = await _dbContext.ActivityLogs
                .Where(a => a.WorkspaceId == workspaceId)
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .ToListAsync();

            return logs.Select(MapToLogViewModel).ToList();
        }

        public async Task<List<ActivityLogViewModel>> GetProjectActivityAsync(Guid projectId)
        {
            var logs = await _dbContext.ActivityLogs
                .Where(a => a.ProjectId == projectId)
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .ToListAsync();

            return logs.Select(MapToLogViewModel).ToList();
        }

        public async Task<List<ActivityLogViewModel>> GetTaskActivityAsync(Guid taskId)
        {
            var logs = await _dbContext.ActivityLogs
                .Where(a => a.TaskId == taskId)
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .ToListAsync();

            return logs.Select(MapToLogViewModel).ToList();
        }

        public async Task<bool> LogActivityAsync(Guid actorUserId, CreateActivityLogRequestViewModel model)
        {
            var log = new ActivityLog
            {
                Id = Guid.NewGuid(),
                WorkspaceId = model.WorkspaceId,
                ProjectId = model.ProjectId,
                TaskId = model.TaskId,
                ActorUserId = actorUserId,
                Action = model.Action,
                Details = model.Details,
                Timestamp = DateTime.UtcNow
            };

            _dbContext.ActivityLogs.Add(log);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<List<NotificationItemViewModel>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false)
        {
            var query = _dbContext.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return notifications.Select(n => new NotificationItemViewModel
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }).ToList();
        }

        public async Task<bool> SendNotificationAsync(SendNotificationRequestViewModel model)
        {
            var item = new NotificationItem
            {
                Id = Guid.NewGuid(),
                UserId = model.UserId,
                Title = model.Title,
                Message = model.Message,
                LinkUrl = model.LinkUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(item);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var item = await _dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (item != null)
            {
                item.IsRead = true;
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(Guid userId)
        {
            var unreadItems = await _dbContext.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var item in unreadItems)
            {
                item.IsRead = true;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        private static ActivityLogViewModel MapToLogViewModel(ActivityLog a)
        {
            return new ActivityLogViewModel
            {
                Id = a.Id,
                WorkspaceId = a.WorkspaceId,
                ProjectId = a.ProjectId,
                TaskId = a.TaskId,
                ActorUserId = a.ActorUserId,
                ActorName = "User",
                Action = a.Action,
                Details = a.Details,
                Timestamp = a.Timestamp
            };
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Modules.TimeTracking.Domain.Entities;
using Modules.TimeTracking.Domain.IServices;
using Modules.TimeTracking.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.TimeTracking;

namespace Modules.TimeTracking.Application.Services
{
    public class TimeTrackingService : ITimeTrackingService
    {
        private readonly TimeTrackingDbContext _dbContext;

        public TimeTrackingService(TimeTrackingDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TimeLogViewModel> StartTimerAsync(Guid userId, Guid taskId)
        {
            var taskExists = await _dbContext.TaskLookups.AnyAsync(t => t.Id == taskId);
            if (!taskExists)
            {
                throw new DomainException("Task not found.");
            }

            // BR-23-01: starting a new timer auto-stops any timer already running for this user.
            var active = await _dbContext.TimeLogs.FirstOrDefaultAsync(t => t.UserId == userId && t.EndedAt == null);
            if (active != null)
            {
                StopInternal(active, null);
                await _dbContext.SaveChangesAsync();
            }

            var timeLog = new TimeLog
            {
                TaskId = taskId,
                UserId = userId,
                StartedAt = DateTime.UtcNow
            };

            _dbContext.TimeLogs.Add(timeLog);
            await _dbContext.SaveChangesAsync();

            return await ToViewModelAsync(timeLog);
        }

        public async Task<TimeLogViewModel> StopTimerAsync(Guid userId, string? notes)
        {
            var active = await _dbContext.TimeLogs.FirstOrDefaultAsync(t => t.UserId == userId && t.EndedAt == null);
            if (active == null)
            {
                throw new DomainException("No active timer to stop.");
            }

            StopInternal(active, notes);
            await _dbContext.SaveChangesAsync();

            return await ToViewModelAsync(active);
        }

        public async Task<ActiveTimerViewModel?> GetActiveTimerAsync(Guid userId)
        {
            var active = await _dbContext.TimeLogs.FirstOrDefaultAsync(t => t.UserId == userId && t.EndedAt == null);
            if (active == null)
            {
                return null;
            }

            var taskTitle = await _dbContext.TaskLookups
                .Where(t => t.Id == active.TaskId)
                .Select(t => t.Title)
                .FirstOrDefaultAsync() ?? "Untitled Task";

            return new ActiveTimerViewModel
            {
                Id = active.Id,
                TaskId = active.TaskId,
                TaskTitle = taskTitle,
                StartedAt = active.StartedAt
            };
        }

        public async Task<List<TimeLogViewModel>> GetTaskTimeLogsAsync(Guid taskId)
        {
            var logs = await _dbContext.TimeLogs
                .Where(t => t.TaskId == taskId)
                .OrderByDescending(t => t.StartedAt)
                .ToListAsync();

            var taskTitle = await _dbContext.TaskLookups
                .Where(t => t.Id == taskId)
                .Select(t => t.Title)
                .FirstOrDefaultAsync() ?? "Untitled Task";

            var userIds = logs.Select(l => l.UserId).Distinct().ToList();
            var userNames = await _dbContext.UserLookups
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            return logs.Select(l => new TimeLogViewModel
            {
                Id = l.Id,
                TaskId = l.TaskId,
                TaskTitle = taskTitle,
                UserId = l.UserId,
                UserName = userNames.TryGetValue(l.UserId, out var name) ? name : "Unknown",
                StartedAt = l.StartedAt,
                EndedAt = l.EndedAt,
                DurationMinutes = l.DurationMinutes,
                Notes = l.Notes
            }).ToList();
        }

        public async Task<TimeLogViewModel> UpdateNotesAsync(Guid userId, Guid timeLogId, string? notes)
        {
            var log = await _dbContext.TimeLogs.FirstOrDefaultAsync(t => t.Id == timeLogId);
            if (log == null)
            {
                throw new DomainException("Time log not found.");
            }
            if (log.UserId != userId)
            {
                throw new PermissionDeniedException("You can only edit notes on your own time logs.");
            }

            log.Notes = notes;
            await _dbContext.SaveChangesAsync();

            return await ToViewModelAsync(log);
        }

        public async Task<TimeTrackingReportViewModel> GetReportAsync(Guid userId, Guid workspaceId, DateTime from, DateTime to)
        {
            var workspace = await _dbContext.WorkspaceLookups.FirstOrDefaultAsync(w => w.Id == workspaceId);
            if (workspace == null)
            {
                throw new DomainException("Workspace not found.");
            }
            if (workspace.OwnerUserId != userId)
            {
                throw new PermissionDeniedException("Only the workspace owner can view this report.");
            }

            var projectIds = await _dbContext.ProjectLookups
                .Where(p => p.WorkspaceId == workspaceId)
                .Select(p => p.Id)
                .ToListAsync();

            var taskLookups = await _dbContext.TaskLookups
                .Where(t => projectIds.Contains(t.ProjectId))
                .ToListAsync();
            var taskIds = taskLookups.Select(t => t.Id).ToList();

            var logsInRange = await _dbContext.TimeLogs
                .Where(t => taskIds.Contains(t.TaskId) && t.StartedAt >= from && t.StartedAt <= to)
                .ToListAsync();

            var activeCount = await _dbContext.TimeLogs
                .Where(t => taskIds.Contains(t.TaskId) && t.EndedAt == null)
                .Select(t => t.UserId)
                .Distinct()
                .CountAsync();

            var userIds = logsInRange.Select(l => l.UserId).Distinct().ToList();
            var userNames = await _dbContext.UserLookups
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            var projectNames = await _dbContext.ProjectLookups
                .Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            var taskToProject = taskLookups.ToDictionary(t => t.Id, t => t.ProjectId);
            var taskTitles = taskLookups.ToDictionary(t => t.Id, t => t.Title);

            var totalMinutes = logsInRange.Sum(l => l.DurationMinutes ?? 0);

            var mostActiveProjectId = logsInRange
                .Where(l => l.DurationMinutes.HasValue && taskToProject.ContainsKey(l.TaskId))
                .GroupBy(l => taskToProject[l.TaskId])
                .Select(g => new { ProjectId = g.Key, Minutes = g.Sum(l => l.DurationMinutes ?? 0) })
                .OrderByDescending(g => g.Minutes)
                .FirstOrDefault();

            var rows = logsInRange
                .OrderByDescending(l => l.StartedAt)
                .Select(l => new TimeTrackingReportRowViewModel
                {
                    Id = l.Id,
                    TeamMember = userNames.TryGetValue(l.UserId, out var name) ? name : "Unknown",
                    TaskName = taskTitles.TryGetValue(l.TaskId, out var title) ? title : "Untitled Task",
                    ProjectName = taskToProject.TryGetValue(l.TaskId, out var pid) && projectNames.TryGetValue(pid, out var pname) ? pname : "Unknown Project",
                    StartTime = l.StartedAt,
                    EndTime = l.EndedAt,
                    DurationMinutes = l.DurationMinutes,
                    Notes = l.Notes
                })
                .ToList();

            return new TimeTrackingReportViewModel
            {
                TotalHoursLogged = Math.Round(totalMinutes / 60.0, 2),
                ActiveUsersCurrentlyTracking = activeCount,
                MostActiveProject = mostActiveProjectId != null && projectNames.TryGetValue(mostActiveProjectId.ProjectId, out var mostName) ? mostName : "—",
                Rows = rows
            };
        }

        private static void StopInternal(TimeLog log, string? notes)
        {
            log.EndedAt = DateTime.UtcNow;
            log.DurationMinutes = (int)Math.Round((log.EndedAt.Value - log.StartedAt).TotalMinutes);
            if (notes != null)
            {
                log.Notes = notes;
            }
        }

        private async Task<TimeLogViewModel> ToViewModelAsync(TimeLog log)
        {
            var taskTitle = await _dbContext.TaskLookups
                .Where(t => t.Id == log.TaskId)
                .Select(t => t.Title)
                .FirstOrDefaultAsync() ?? "Untitled Task";

            var userName = await _dbContext.UserLookups
                .Where(u => u.Id == log.UserId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync() ?? "Unknown";

            return new TimeLogViewModel
            {
                Id = log.Id,
                TaskId = log.TaskId,
                TaskTitle = taskTitle,
                UserId = log.UserId,
                UserName = userName,
                StartedAt = log.StartedAt,
                EndedAt = log.EndedAt,
                DurationMinutes = log.DurationMinutes,
                Notes = log.Notes
            };
        }
    }
}

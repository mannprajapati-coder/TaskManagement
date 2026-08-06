using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Tasks.Domain.Entities;
using Modules.Tasks.Domain.IServices;
using Modules.Tasks.Infrastructure.Context;
using TaskPlatform.Shared.Exceptions;
using TaskPlatform.Shared.ViewModels.Task;

namespace Modules.Tasks.Application.Services
{
    public class TasksService : ITasksService
    {
        private readonly TasksDbContext _dbContext;

        public TasksService(TasksDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<TaskViewModel>> GetProjectTasksAsync(Guid projectId, string? status = null, string? priority = null)
        {
            var query = _dbContext.Tasks
                .Where(t => t.ProjectId == projectId && t.ParentTaskId == null);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrEmpty(priority))
            {
                query = query.Where(t => t.Priority == priority);
            }

            var tasks = await query
                .OrderBy(t => t.SortOrder)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(tasks
                .Where(t => t.PrimaryAssigneeUserId.HasValue)
                .Select(t => t.PrimaryAssigneeUserId!.Value));

            return tasks.Select(t => MapToViewModel(t, userLookup)).ToList();
        }

        public async Task<List<TaskViewModel>> GetMyTasksAsync(Guid userId, Guid? projectId = null)
        {
            var query = _dbContext.TaskAssignees
                .Where(a => a.UserId == userId)
                .Join(_dbContext.Tasks, a => a.TaskId, t => t.Id, (a, t) => t)
                .Where(t => t.ParentTaskId == null);

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            var tasks = await query
                .OrderBy(t => t.DueDate == null)
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(tasks
                .Where(t => t.PrimaryAssigneeUserId.HasValue)
                .Select(t => t.PrimaryAssigneeUserId!.Value));

            return tasks.Select(t => MapToViewModel(t, userLookup)).ToList();
        }

        public async Task<TaskViewModel> GetTaskByIdAsync(Guid taskId)
        {
            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null)
            {
                throw new DomainException("Task not found.");
            }

            return await MapSingleToViewModelAsync(task);
        }

        public async Task<TaskViewModel> CreateTaskAsync(Guid userId, CreateTaskRequestViewModel model)
        {
            ValidateTaskDates(model.StartDate, model.DueDate);

            var sortOrder = await _dbContext.Tasks
                .CountAsync(t => t.ProjectId == model.ProjectId && t.Status == "Todo" && t.ParentTaskId == null);

            var task = new TaskEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = model.ProjectId,
                Title = model.Title,
                Description = model.Description,
                Status = "Todo",
                SortOrder = sortOrder,
                Priority = string.IsNullOrEmpty(model.Priority) ? "Medium" : model.Priority,
                StartDate = model.StartDate,
                DueDate = model.DueDate,
                EstimatedHours = model.EstimatedHours,
                ActualHours = 0,
                PrimaryAssigneeUserId = model.PrimaryAssigneeUserId,
                CreatorUserId = userId,
                CompletedAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Tasks.Add(task);

            if (model.PrimaryAssigneeUserId.HasValue)
            {
                _dbContext.TaskAssignees.Add(new TaskAssignee
                {
                    Id = Guid.NewGuid(),
                    TaskId = task.Id,
                    UserId = model.PrimaryAssigneeUserId.Value,
                    IsPrimary = true,
                    AssignedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();

            return await MapSingleToViewModelAsync(task);
        }

        public async Task<TaskViewModel> UpdateTaskAsync(Guid userId, UpdateTaskRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                throw new DomainException("Task not found.");
            }

            if (model.ActualHours.HasValue && model.ActualHours.Value < 0)
            {
                throw new DomainException("Actual hours must be non-negative.");
            }

            ValidateTaskDates(model.StartDate, model.DueDate);

            var oldPrimary = task.PrimaryAssigneeUserId;

            task.Title = model.Title;
            task.Description = model.Description;
            task.Priority = model.Priority;
            task.StartDate = model.StartDate;
            task.DueDate = model.DueDate;
            task.EstimatedHours = model.EstimatedHours;

            if (model.ActualHours.HasValue)
            {
                task.ActualHours = model.ActualHours.Value;
            }

            task.PrimaryAssigneeUserId = model.PrimaryAssigneeUserId;
            task.UpdatedAt = DateTime.UtcNow;

            if (model.PrimaryAssigneeUserId.HasValue && oldPrimary != model.PrimaryAssigneeUserId)
            {
                var existingAssignees = await _dbContext.TaskAssignees
                    .Where(a => a.TaskId == task.Id)
                    .ToListAsync();

                foreach (var a in existingAssignees)
                {
                    a.IsPrimary = false;
                }

                var newPrimary = existingAssignees.FirstOrDefault(a => a.UserId == model.PrimaryAssigneeUserId.Value);
                if (newPrimary != null)
                {
                    newPrimary.IsPrimary = true;
                }
                else
                {
                    _dbContext.TaskAssignees.Add(new TaskAssignee
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task.Id,
                        UserId = model.PrimaryAssigneeUserId.Value,
                        IsPrimary = true,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
            return await MapSingleToViewModelAsync(task);
        }

        public async Task<TaskViewModel> UpdateTaskStatusAsync(Guid userId, UpdateTaskStatusRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                throw new DomainException("Task not found.");
            }

            if (!task.Status.Equals(model.Status, StringComparison.OrdinalIgnoreCase))
            {
                await ApplyStatusChangeAsync(task, model.Status);
            }

            await _dbContext.SaveChangesAsync();
            return await MapSingleToViewModelAsync(task);
        }

        public async Task<TaskViewModel> ReorderTasksAsync(Guid userId, ReorderTasksRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                throw new DomainException("Task not found.");
            }

            if (!task.Status.Equals(model.Status, StringComparison.OrdinalIgnoreCase))
            {
                await ApplyStatusChangeAsync(task, model.Status);
            }

            var columnTasks = await _dbContext.Tasks
                .Where(t => model.OrderedTaskIds.Contains(t.Id))
                .ToListAsync();

            for (var i = 0; i < model.OrderedTaskIds.Count; i++)
            {
                var columnTask = columnTasks.FirstOrDefault(t => t.Id == model.OrderedTaskIds[i]);
                if (columnTask != null)
                {
                    columnTask.SortOrder = i;
                }
            }

            await _dbContext.SaveChangesAsync();
            return await MapSingleToViewModelAsync(task);
        }

        private async Task ApplyStatusChangeAsync(TaskEntity task, string newStatus)
        {
            if (newStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                var incompleteSubtasksExist = await _dbContext.Tasks
                    .AnyAsync(st => st.ParentTaskId == task.Id &&
                                    st.Status != "Completed" &&
                                    st.Status != "Cancelled");

                if (incompleteSubtasksExist)
                {
                    throw new DomainException("Cannot mark parent task as Completed while incomplete subtasks remain.");
                }

                if (!task.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    task.CompletedAt = DateTime.UtcNow;
                }
            }
            else
            {
                task.CompletedAt = null;
            }

            var maxSortOrder = await _dbContext.Tasks
                .Where(t => t.ProjectId == task.ProjectId && t.Status == newStatus && t.ParentTaskId == null && t.Id != task.Id)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync();

            task.Status = newStatus;
            task.SortOrder = (maxSortOrder ?? -1) + 1;
            task.UpdatedAt = DateTime.UtcNow;
        }

        public async Task<bool> DeleteTaskAsync(Guid userId, Guid taskId)
        {
            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task != null)
            {
                _dbContext.Tasks.Remove(task);
                await _dbContext.SaveChangesAsync();
            }
            return true;
        }

        // Sprint 05: Subtasks Implementation
        public async Task<List<SubtaskViewModel>> GetSubtasksAsync(Guid parentTaskId)
        {
            var subtasks = await _dbContext.Tasks
                .Where(t => t.ParentTaskId == parentTaskId)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            return subtasks.Select(st => new SubtaskViewModel
            {
                Id = st.Id,
                ParentTaskId = st.ParentTaskId!.Value,
                ProjectId = st.ProjectId,
                Title = st.Title,
                Status = st.Status,
                Priority = st.Priority,
                DueDate = st.DueDate,
                PrimaryAssigneeUserId = st.PrimaryAssigneeUserId,
                CreatedAt = st.CreatedAt
            }).ToList();
        }

        public async Task<SubtaskViewModel> CreateSubtaskAsync(Guid userId, CreateSubtaskRequestViewModel model)
        {
            var parentTask = await _dbContext.Tasks.FindAsync(model.ParentTaskId);
            if (parentTask == null)
            {
                throw new DomainException("Parent task not found.");
            }

            var subtask = new TaskEntity
            {
                Id = Guid.NewGuid(),
                ParentTaskId = model.ParentTaskId,
                ProjectId = model.ProjectId,
                Title = model.Title,
                Description = null,
                Status = "Todo",
                Priority = string.IsNullOrEmpty(model.Priority) ? "Medium" : model.Priority,
                StartDate = null,
                DueDate = model.DueDate,
                EstimatedHours = null,
                ActualHours = 0,
                PrimaryAssigneeUserId = model.PrimaryAssigneeUserId,
                CreatorUserId = userId,
                CompletedAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Tasks.Add(subtask);
            await _dbContext.SaveChangesAsync();

            return new SubtaskViewModel
            {
                Id = subtask.Id,
                ParentTaskId = subtask.ParentTaskId.Value,
                ProjectId = subtask.ProjectId,
                Title = subtask.Title,
                Status = subtask.Status,
                Priority = subtask.Priority,
                DueDate = subtask.DueDate,
                PrimaryAssigneeUserId = subtask.PrimaryAssigneeUserId,
                CreatedAt = subtask.CreatedAt
            };
        }

        public async Task<bool> DeleteTaskWithSubtasksAsync(Guid userId, Guid parentTaskId)
        {
            var parentTask = await _dbContext.Tasks.FindAsync(parentTaskId);
            if (parentTask != null)
            {
                var subtasks = await _dbContext.Tasks
                    .Where(t => t.ParentTaskId == parentTaskId)
                    .ToListAsync();

                _dbContext.Tasks.RemoveRange(subtasks);
                _dbContext.Tasks.Remove(parentTask);
                await _dbContext.SaveChangesAsync();
            }
            return true;
        }

        // Sprint 05: Multi-Assignees & Watchers Implementation
        public async Task<List<TaskAssigneeViewModel>> GetTaskAssigneesAsync(Guid taskId)
        {
            var assignees = await _dbContext.TaskAssignees
                .Where(a => a.TaskId == taskId)
                .OrderByDescending(a => a.IsPrimary)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(assignees.Select(a => a.UserId));

            return assignees.Select(a => new TaskAssigneeViewModel
            {
                Id = a.Id,
                TaskId = a.TaskId,
                UserId = a.UserId,
                FullName = userLookup.TryGetValue(a.UserId, out var u) ? u.FullName : "Unknown User",
                Email = userLookup.TryGetValue(a.UserId, out var u2) ? (u2.Email ?? string.Empty) : string.Empty,
                IsPrimary = a.IsPrimary,
                AssignedAt = a.AssignedAt
            }).ToList();
        }

        private async Task<Dictionary<Guid, UserLookup>> GetUserLookupAsync(IEnumerable<Guid> userIds)
        {
            var distinctIds = userIds.Distinct().ToList();
            return await _dbContext.UserLookups
                .Where(u => distinctIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
        }

        public async Task<TaskAssigneeViewModel> AddTaskAssigneeAsync(Guid userId, AddTaskAssigneeRequestViewModel model)
        {
            var existing = await _dbContext.TaskAssignees
                .FirstOrDefaultAsync(a => a.TaskId == model.TaskId && a.UserId == model.UserId);

            if (existing != null)
            {
                throw new DomainException("User is already assigned to this task.");
            }

            var assignee = new TaskAssignee
            {
                Id = Guid.NewGuid(),
                TaskId = model.TaskId,
                UserId = model.UserId,
                IsPrimary = model.IsPrimary,
                AssignedAt = DateTime.UtcNow
            };

            _dbContext.TaskAssignees.Add(assignee);
            await _dbContext.SaveChangesAsync();

            var assignedUser = await _dbContext.UserLookups.FirstOrDefaultAsync(u => u.Id == assignee.UserId);

            return new TaskAssigneeViewModel
            {
                Id = assignee.Id,
                TaskId = assignee.TaskId,
                UserId = assignee.UserId,
                FullName = assignedUser?.FullName ?? "Unknown User",
                Email = assignedUser?.Email ?? string.Empty,
                IsPrimary = assignee.IsPrimary,
                AssignedAt = assignee.AssignedAt
            };
        }

        public async Task<bool> RemoveTaskAssigneeAsync(Guid userId, Guid taskId, Guid targetUserId)
        {
            var assignee = await _dbContext.TaskAssignees
                .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == targetUserId);

            if (assignee != null)
            {
                _dbContext.TaskAssignees.Remove(assignee);
                await _dbContext.SaveChangesAsync();
            }

            return true;
        }

        public async Task<List<TaskWatcherViewModel>> GetTaskWatchersAsync(Guid taskId)
        {
            var watchers = await _dbContext.TaskWatchers
                .Where(w => w.TaskId == taskId)
                .OrderByDescending(w => w.WatchingSince)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(watchers.Select(w => w.UserId));

            return watchers.Select(w => new TaskWatcherViewModel
            {
                Id = w.Id,
                TaskId = w.TaskId,
                UserId = w.UserId,
                FullName = userLookup.TryGetValue(w.UserId, out var u) ? u.FullName : "Unknown User",
                Email = userLookup.TryGetValue(w.UserId, out var u2) ? (u2.Email ?? string.Empty) : string.Empty,
                WatchingSince = w.WatchingSince
            }).ToList();
        }

        public async Task<bool> ToggleTaskWatcherAsync(Guid userId, Guid taskId)
        {
            var existing = await _dbContext.TaskWatchers
                .FirstOrDefaultAsync(w => w.TaskId == taskId && w.UserId == userId);

            if (existing == null)
            {
                _dbContext.TaskWatchers.Add(new TaskWatcher
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    UserId = userId,
                    WatchingSince = DateTime.UtcNow
                });
            }
            else
            {
                _dbContext.TaskWatchers.Remove(existing);
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Sprint 06: Checklists & Recurring Tasks Implementation
        public async Task<List<ChecklistItemViewModel>> GetChecklistItemsAsync(Guid taskId)
        {
            var items = await _dbContext.ChecklistItems
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.CreatedAt)
                .ToListAsync();

            return items.Select(c => new ChecklistItemViewModel
            {
                Id = c.Id,
                TaskId = c.TaskId,
                Title = c.Title,
                IsCompleted = c.IsCompleted,
                SortOrder = c.SortOrder,
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task<ChecklistItemViewModel> AddChecklistItemAsync(Guid userId, AddChecklistItemRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null) throw new DomainException("Task not found.");

            var count = await _dbContext.ChecklistItems.CountAsync(c => c.TaskId == model.TaskId);

            var item = new ChecklistItem
            {
                Id = Guid.NewGuid(),
                TaskId = model.TaskId,
                Title = model.Title,
                IsCompleted = false,
                SortOrder = count + 1,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ChecklistItems.Add(item);
            await _dbContext.SaveChangesAsync();

            return new ChecklistItemViewModel
            {
                Id = item.Id,
                TaskId = item.TaskId,
                Title = item.Title,
                IsCompleted = item.IsCompleted,
                SortOrder = item.SortOrder,
                CreatedAt = item.CreatedAt
            };
        }

        public async Task<bool> ToggleChecklistItemAsync(Guid userId, Guid itemId)
        {
            var item = await _dbContext.ChecklistItems.FindAsync(itemId);
            if (item == null) throw new DomainException("Checklist item not found.");

            item.IsCompleted = !item.IsCompleted;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteChecklistItemAsync(Guid userId, Guid itemId)
        {
            var item = await _dbContext.ChecklistItems.FindAsync(itemId);
            if (item != null)
            {
                _dbContext.ChecklistItems.Remove(item);
                await _dbContext.SaveChangesAsync();
            }
            return true;
        }

        public async Task<RecurringTaskRuleViewModel> SetRecurringTaskRuleAsync(Guid userId, SetRecurringTaskRuleRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null) throw new DomainException("Task not found.");

            var existingRule = await _dbContext.RecurringTaskRules
                .FirstOrDefaultAsync(r => r.TaskId == model.TaskId);

            if (existingRule == null)
            {
                existingRule = new RecurringTaskRule
                {
                    Id = Guid.NewGuid(),
                    TaskId = model.TaskId,
                    RecurrencePattern = model.RecurrencePattern,
                    Interval = model.Interval,
                    DaysOfWeek = model.DaysOfWeek,
                    NextRunDate = model.StartRunDate,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.RecurringTaskRules.Add(existingRule);
            }
            else
            {
                existingRule.RecurrencePattern = model.RecurrencePattern;
                existingRule.Interval = model.Interval;
                existingRule.DaysOfWeek = model.DaysOfWeek;
                existingRule.NextRunDate = model.StartRunDate;
                existingRule.IsActive = true;
            }

            await _dbContext.SaveChangesAsync();

            return new RecurringTaskRuleViewModel
            {
                Id = existingRule.Id,
                TaskId = existingRule.TaskId,
                RecurrencePattern = existingRule.RecurrencePattern,
                Interval = existingRule.Interval,
                DaysOfWeek = existingRule.DaysOfWeek,
                NextRunDate = existingRule.NextRunDate,
                IsActive = existingRule.IsActive
            };
        }

        public async Task<RecurringTaskRuleViewModel?> GetRecurringTaskRuleAsync(Guid taskId)
        {
            var rule = await _dbContext.RecurringTaskRules
                .FirstOrDefaultAsync(r => r.TaskId == taskId);

            if (rule == null) return null;

            return new RecurringTaskRuleViewModel
            {
                Id = rule.Id,
                TaskId = rule.TaskId,
                RecurrencePattern = rule.RecurrencePattern,
                Interval = rule.Interval,
                DaysOfWeek = rule.DaysOfWeek,
                NextRunDate = rule.NextRunDate,
                IsActive = rule.IsActive
            };
        }

        public async Task<int> ProcessDueRecurringTasksAsync()
        {
            var now = DateTime.UtcNow;
            var dueRules = await _dbContext.RecurringTaskRules
                .Include(r => r.Task)
                .Where(r => r.IsActive && r.NextRunDate <= now)
                .ToListAsync();

            int createdCount = 0;
            foreach (var rule in dueRules)
            {
                if (rule.Task == null) continue;

                var newTask = new TaskEntity
                {
                    Id = Guid.NewGuid(),
                    ProjectId = rule.Task.ProjectId,
                    Title = $"{rule.Task.Title} (Recurring)",
                    Description = rule.Task.Description,
                    Status = "Todo",
                    Priority = rule.Task.Priority,
                    StartDate = now,
                    DueDate = now.AddDays(1),
                    EstimatedHours = rule.Task.EstimatedHours,
                    ActualHours = 0,
                    PrimaryAssigneeUserId = rule.Task.PrimaryAssigneeUserId,
                    CreatorUserId = rule.Task.CreatorUserId,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _dbContext.Tasks.Add(newTask);

                // Compute next run date
                rule.NextRunDate = rule.RecurrencePattern switch
                {
                    "Weekly" => rule.NextRunDate.AddDays(7 * rule.Interval),
                    "Monthly" => rule.NextRunDate.AddMonths(rule.Interval),
                    _ => rule.NextRunDate.AddDays(rule.Interval)
                };

                createdCount++;
            }

            await _dbContext.SaveChangesAsync();
            return createdCount;
        }

        private static void ValidateTaskDates(DateTime? startDate, DateTime? dueDate)
        {
            if (startDate.HasValue && dueDate.HasValue && dueDate.Value < startDate.Value)
            {
                throw new DomainException("Task Due Date must be on or after Start Date.");
            }
        }

        private async Task<TaskViewModel> MapSingleToViewModelAsync(TaskEntity t)
        {
            IReadOnlyDictionary<Guid, UserLookup> userLookup = t.PrimaryAssigneeUserId.HasValue
                ? await GetUserLookupAsync(new[] { t.PrimaryAssigneeUserId.Value })
                : new Dictionary<Guid, UserLookup>();

            return MapToViewModel(t, userLookup);
        }

        private static TaskViewModel MapToViewModel(TaskEntity t, IReadOnlyDictionary<Guid, UserLookup> userLookup)
        {
            var primaryAssigneeName = t.PrimaryAssigneeUserId.HasValue
                ? (userLookup.TryGetValue(t.PrimaryAssigneeUserId.Value, out var assignee) ? assignee.FullName : "Unknown User")
                : "Unassigned";

            return new TaskViewModel
            {
                Id = t.Id,
                ProjectId = t.ProjectId,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                SortOrder = t.SortOrder,
                Priority = t.Priority,
                StartDate = t.StartDate,
                DueDate = t.DueDate,
                EstimatedHours = t.EstimatedHours,
                ActualHours = t.ActualHours,
                PrimaryAssigneeUserId = t.PrimaryAssigneeUserId,
                PrimaryAssigneeName = primaryAssigneeName,
                CreatorUserId = t.CreatorUserId,
                CompletedAt = t.CompletedAt,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            };
        }
    }
}

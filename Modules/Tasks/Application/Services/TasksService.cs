using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modules.Projects.Domain.IServices;
using Modules.Tasks.Domain.Entities;
using Modules.Tasks.Domain.IServices;
using Modules.Tasks.Infrastructure.Context;
using Modules.Workspaces.Domain.IServices;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.Task;

namespace Modules.Tasks.Application.Services
{
    public class TasksService : ITasksService
    {
        private readonly TasksDbContext _dbContext;
        private readonly IProjectsService _projectsService;
        private readonly IWorkspaceService _workspaceService;

        public TasksService(TasksDbContext dbContext, IProjectsService projectsService, IWorkspaceService workspaceService)
        {
            _dbContext = dbContext;
            _projectsService = projectsService;
            _workspaceService = workspaceService;
        }

        private async Task<string?> CheckCanModifyTaskAsync(Guid userId, TaskEntity task)
        {
            var isAssignee = await _dbContext.TaskAssignees
                .AnyAsync(a => a.TaskId == task.Id && a.UserId == userId);
            if (isAssignee) return null;

            var project = await _projectsService.GetProjectByIdAsync(task.ProjectId, userId);
            if (project != null)
            {
                var isProjectOwner = (await _projectsService.GetProjectMembersAsync(project.Id, userId))
                    .Any(m => m.UserId == userId && m.Role == "Owner");
                if (isProjectOwner) return null;

                try
                {
                    var workspace = await _workspaceService.GetWorkspaceByIdAsync(project.WorkspaceId, userId);
                    if (workspace != null && workspace.OwnerUserId == userId) return null;
                }
                catch
                {
                    // Not a workspace member
                }
            }

            return "Only an assignee, the project owner, or the workspace owner can modify this task.";
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
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(tasks.Where(t => t.PrimaryAssigneeUserId.HasValue).Select(t => t.PrimaryAssigneeUserId!.Value));

            return tasks.Select(t => MapToViewModel(t, userLookup)).ToList();
        }

        public async Task<List<TaskViewModel>> GetMyTasksAsync(Guid userId, Guid? projectId = null)
        {
            var assignedTaskIds = await _dbContext.TaskAssignees
                .Where(a => a.UserId == userId)
                .Select(a => a.TaskId)
                .ToListAsync();

            var query = _dbContext.Tasks
                .Where(t => (t.PrimaryAssigneeUserId == userId || assignedTaskIds.Contains(t.Id)) && t.ParentTaskId == null);

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            var tasks = await query
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(tasks.Where(t => t.PrimaryAssigneeUserId.HasValue).Select(t => t.PrimaryAssigneeUserId!.Value));

            return tasks.Select(t => MapToViewModel(t, userLookup)).ToList();
        }

        public async Task<ApiResponse<TaskViewModel>> GetTaskByIdAsync(Guid taskId)
        {
            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null)
            {
                return ApiResponse<TaskViewModel>.Fail("Task not found.");
            }

            var vm = await MapSingleToViewModelAsync(task);
            return ApiResponse<TaskViewModel>.Ok(vm);
        }

        public async Task<ApiResponse<TaskViewModel>> CreateTaskAsync(Guid userId, CreateTaskRequestViewModel model)
        {
            var dateError = ValidateTaskDates(model.StartDate, model.DueDate);
            if (dateError != null)
            {
                return ApiResponse<TaskViewModel>.Fail(dateError);
            }

            var maxSortOrder = await _dbContext.Tasks
                .Where(t => t.ProjectId == model.ProjectId && t.Status == "Todo" && t.ParentTaskId == null)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync();

            var task = new TaskEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = model.ProjectId,
                Title = model.Title,
                Description = model.Description,
                Status = "Todo",
                SortOrder = (maxSortOrder ?? -1) + 1,
                Priority = model.Priority ?? "Medium",
                StartDate = model.StartDate,
                DueDate = model.DueDate,
                EstimatedHours = model.EstimatedHours,
                ActualHours = 0,
                PrimaryAssigneeUserId = model.PrimaryAssigneeUserId,
                CreatorUserId = userId,
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
                    AssignedAt = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();
            var vm = await MapSingleToViewModelAsync(task);
            return ApiResponse<TaskViewModel>.Ok(vm, "Task created successfully.");
        }

        public async Task<ApiResponse<TaskViewModel>> UpdateTaskAsync(Guid userId, UpdateTaskRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                return ApiResponse<TaskViewModel>.Fail("Task not found.");
            }

            var dateError = ValidateTaskDates(model.StartDate, model.DueDate);
            if (dateError != null)
            {
                return ApiResponse<TaskViewModel>.Fail(dateError);
            }

            if (model.ActualHours.HasValue && model.ActualHours.Value < 0)
            {
                return ApiResponse<TaskViewModel>.Fail("Actual hours must be non-negative.");
            }

            var canModifyError = await CheckCanModifyTaskAsync(userId, task);
            if (canModifyError != null)
            {
                return ApiResponse<TaskViewModel>.Fail(canModifyError);
            }

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


            if (task.PrimaryAssigneeUserId != model.PrimaryAssigneeUserId)
            {
                task.PrimaryAssigneeUserId = model.PrimaryAssigneeUserId;
                if (model.PrimaryAssigneeUserId.HasValue)
                {
                    var exists = await _dbContext.TaskAssignees
                        .AnyAsync(a => a.TaskId == task.Id && a.UserId == model.PrimaryAssigneeUserId.Value);
                    if (!exists)
                    {
                        _dbContext.TaskAssignees.Add(new TaskAssignee
                        {
                            Id = Guid.NewGuid(),
                            TaskId = task.Id,
                            UserId = model.PrimaryAssigneeUserId.Value,
                            AssignedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            task.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            var vm = await MapSingleToViewModelAsync(task);
            return ApiResponse<TaskViewModel>.Ok(vm, "Task updated successfully.");
        }

        public async Task<ApiResponse<TaskViewModel>> UpdateTaskStatusAsync(Guid userId, UpdateTaskStatusRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                return ApiResponse<TaskViewModel>.Fail("Task not found.");
            }

            var canModifyError = await CheckCanModifyTaskAsync(userId, task);
            if (canModifyError != null)
            {
                return ApiResponse<TaskViewModel>.Fail(canModifyError);
            }

            if (!task.Status.Equals(model.Status, StringComparison.OrdinalIgnoreCase))
            {
                var statusError = await ApplyStatusChangeAsync(task, model.Status);
                if (statusError != null)
                {
                    return ApiResponse<TaskViewModel>.Fail(statusError);
                }
            }

            await _dbContext.SaveChangesAsync();
            var vm = await MapSingleToViewModelAsync(task);
            return ApiResponse<TaskViewModel>.Ok(vm, "Task status updated.");
        }

        public async Task<ApiResponse<TaskViewModel>> ReorderTasksAsync(Guid userId, ReorderTasksRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                return ApiResponse<TaskViewModel>.Fail("Task not found.");
            }

            var canModifyError = await CheckCanModifyTaskAsync(userId, task);
            if (canModifyError != null)
            {
                return ApiResponse<TaskViewModel>.Fail(canModifyError);
            }

            if (!task.Status.Equals(model.Status, StringComparison.OrdinalIgnoreCase))
            {
                var statusError = await ApplyStatusChangeAsync(task, model.Status);
                if (statusError != null)
                {
                    return ApiResponse<TaskViewModel>.Fail(statusError);
                }
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
            var vm = await MapSingleToViewModelAsync(task);
            return ApiResponse<TaskViewModel>.Ok(vm, "Tasks reordered.");
        }

        private async Task<string?> ApplyStatusChangeAsync(TaskEntity task, string newStatus)
        {
            if (newStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase))
            {
                var incompleteSubtasksExist = await _dbContext.Tasks
                    .AnyAsync(st => st.ParentTaskId == task.Id &&
                                    st.Status != "Completed" &&
                                    st.Status != "Cancelled");

                if (incompleteSubtasksExist)
                {
                    return "Cannot mark parent task as Completed while incomplete subtasks remain.";
                }

                var hasIncompleteChecklist = await _dbContext.ChecklistItems
                    .AnyAsync(c => c.TaskId == task.Id && !c.IsCompleted);

                if (hasIncompleteChecklist)
                {
                    return "Cannot mark task as Completed while checklist items remain incomplete.";
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

            return null;
        }

        public async Task<ApiResponse<bool>> DeleteTaskAsync(Guid userId, Guid taskId)
        {
            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null)
            {
                return ApiResponse<bool>.Fail("Task not found.");
            }

            var canModifyError = await CheckCanModifyTaskAsync(userId, task);
            if (canModifyError != null)
            {
                return ApiResponse<bool>.Fail(canModifyError);
            }

            _dbContext.Tasks.Remove(task);
            await _dbContext.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true, "Task deleted.");
        }

        public async Task<List<SubtaskViewModel>> GetSubtasksAsync(Guid parentTaskId)
        {
            var subtasks = await _dbContext.Tasks
                .Where(t => t.ParentTaskId == parentTaskId)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(subtasks.Where(t => t.PrimaryAssigneeUserId.HasValue).Select(t => t.PrimaryAssigneeUserId!.Value));

            return subtasks.Select(t => new SubtaskViewModel
            {
                Id = t.Id,
                ParentTaskId = t.ParentTaskId!.Value,
                ProjectId = t.ProjectId,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                PrimaryAssigneeUserId = t.PrimaryAssigneeUserId,
                DueDate = t.DueDate,
                EstimatedHours = t.EstimatedHours,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task<ApiResponse<SubtaskViewModel>> CreateSubtaskAsync(Guid userId, CreateSubtaskRequestViewModel model)
        {
            var parentTask = await _dbContext.Tasks.FindAsync(model.ParentTaskId);
            if (parentTask == null)
            {
                return ApiResponse<SubtaskViewModel>.Fail("Parent task not found.");
            }

            var canModifyError = await CheckCanModifyTaskAsync(userId, parentTask);
            if (canModifyError != null)
            {
                return ApiResponse<SubtaskViewModel>.Fail(canModifyError);
            }

            var maxSortOrder = await _dbContext.Tasks
                .Where(t => t.ParentTaskId == model.ParentTaskId)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync();

            var subtask = new TaskEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = parentTask.ProjectId,
                ParentTaskId = model.ParentTaskId,
                Title = model.Title,
                Status = "Todo",
                SortOrder = (maxSortOrder ?? -1) + 1,
                Priority = string.IsNullOrEmpty(model.Priority) ? "Medium" : model.Priority,
                DueDate = model.DueDate,
                EstimatedHours = model.EstimatedHours,
                PrimaryAssigneeUserId = model.PrimaryAssigneeUserId,
                CreatorUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Tasks.Add(subtask);
            await _dbContext.SaveChangesAsync();

            var userLookup = subtask.PrimaryAssigneeUserId.HasValue
                ? await GetUserLookupAsync(new[] { subtask.PrimaryAssigneeUserId.Value })
                : new Dictionary<Guid, UserLookup>();

            var vm = new SubtaskViewModel
            {
                Id = subtask.Id,
                ParentTaskId = subtask.ParentTaskId!.Value,
                ProjectId = subtask.ProjectId,
                Title = subtask.Title,
                Status = subtask.Status,
                Priority = subtask.Priority,
                PrimaryAssigneeUserId = subtask.PrimaryAssigneeUserId,
                DueDate = subtask.DueDate,
                EstimatedHours = subtask.EstimatedHours,
                CreatedAt = subtask.CreatedAt
            };

            return ApiResponse<SubtaskViewModel>.Ok(vm, "Subtask created successfully.");
        }

        public async Task<ApiResponse<bool>> DeleteTaskWithSubtasksAsync(Guid userId, Guid parentTaskId)
        {
            var parentTask = await _dbContext.Tasks.FindAsync(parentTaskId);
            if (parentTask == null)
            {
                return ApiResponse<bool>.Fail("Parent task not found.");
            }

            var canModifyError = await CheckCanModifyTaskAsync(userId, parentTask);
            if (canModifyError != null)
            {
                return ApiResponse<bool>.Fail(canModifyError);
            }

            var subtasks = await _dbContext.Tasks
                .Where(t => t.ParentTaskId == parentTaskId)
                .ToListAsync();

            _dbContext.Tasks.RemoveRange(subtasks);
            _dbContext.Tasks.Remove(parentTask);
            await _dbContext.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Task and subtasks deleted.");
        }

        public async Task<List<TaskAssigneeViewModel>> GetTaskAssigneesAsync(Guid taskId)
        {
            var assignees = await _dbContext.TaskAssignees
                .Where(a => a.TaskId == taskId)
                .OrderBy(a => a.AssignedAt)
                .ToListAsync();

            var userLookup = await GetUserLookupAsync(assignees.Select(a => a.UserId));

            return assignees.Select(a => new TaskAssigneeViewModel
            {
                Id = a.Id,
                TaskId = a.TaskId,
                UserId = a.UserId,
                FullName = userLookup.TryGetValue(a.UserId, out var u) ? u.FullName : "Unknown User",
                Email = userLookup.TryGetValue(a.UserId, out var u2) ? (u2.Email ?? string.Empty) : string.Empty,
                AssignedAt = a.AssignedAt
            }).ToList();
        }

        public async Task<ApiResponse<TaskAssigneeViewModel>> AddTaskAssigneeAsync(Guid userId, AddTaskAssigneeRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                return ApiResponse<TaskAssigneeViewModel>.Fail("Task not found.");
            }

            var canModifyError = await CheckCanModifyTaskAsync(userId, task);
            if (canModifyError != null)
            {
                return ApiResponse<TaskAssigneeViewModel>.Fail(canModifyError);
            }

            var exists = await _dbContext.TaskAssignees
                .AnyAsync(a => a.TaskId == model.TaskId && a.UserId == model.UserId);

            if (exists)
            {
                return ApiResponse<TaskAssigneeViewModel>.Fail("User is already assigned to this task.");
            }

            var assignee = new TaskAssignee
            {
                Id = Guid.NewGuid(),
                TaskId = model.TaskId,
                UserId = model.UserId,
                AssignedAt = DateTime.UtcNow
            };

            _dbContext.TaskAssignees.Add(assignee);
            await _dbContext.SaveChangesAsync();

            var userLookup = await GetUserLookupAsync(new[] { model.UserId });

            var vm = new TaskAssigneeViewModel
            {
                Id = assignee.Id,
                TaskId = assignee.TaskId,
                UserId = assignee.UserId,
                FullName = userLookup.TryGetValue(assignee.UserId, out var u) ? u.FullName : "Unknown User",
                Email = userLookup.TryGetValue(assignee.UserId, out var u2) ? (u2.Email ?? string.Empty) : string.Empty,
                AssignedAt = assignee.AssignedAt
            };

            return ApiResponse<TaskAssigneeViewModel>.Ok(vm, "Assignee added successfully.");
        }

        public async Task<ApiResponse<bool>> RemoveTaskAssigneeAsync(Guid userId, Guid taskId, Guid targetUserId)
        {
            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null)
            {
                return ApiResponse<bool>.Fail("Task not found.");
            }

            var canModifyError = await CheckCanModifyTaskAsync(userId, task);
            if (canModifyError != null)
            {
                return ApiResponse<bool>.Fail(canModifyError);
            }

            var assignee = await _dbContext.TaskAssignees
                .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == targetUserId);

            if (assignee != null)
            {
                _dbContext.TaskAssignees.Remove(assignee);
                await _dbContext.SaveChangesAsync();
            }

            return ApiResponse<bool>.Ok(true, "Assignee removed successfully.");
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

        public async Task<ApiResponse<bool>> ToggleTaskWatcherAsync(Guid userId, Guid taskId)
        {
            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null)
            {
                return ApiResponse<bool>.Fail("Task not found.");
            }

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
            return ApiResponse<bool>.Ok(true);
        }

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

        public async Task<ApiResponse<ChecklistItemViewModel>> AddChecklistItemAsync(Guid userId, AddChecklistItemRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                return ApiResponse<ChecklistItemViewModel>.Fail("Task not found.");
            }

            var canModifyError = await CheckCanModifyTaskAsync(userId, task);
            if (canModifyError != null)
            {
                return ApiResponse<ChecklistItemViewModel>.Fail(canModifyError);
            }

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

            return ApiResponse<ChecklistItemViewModel>.Ok(new ChecklistItemViewModel
            {
                Id = item.Id,
                TaskId = item.TaskId,
                Title = item.Title,
                IsCompleted = item.IsCompleted,
                SortOrder = item.SortOrder,
                CreatedAt = item.CreatedAt
            }, "Checklist item added.");
        }

        public async Task<ApiResponse<bool>> ToggleChecklistItemAsync(Guid userId, Guid itemId)
        {
            var item = await _dbContext.ChecklistItems.FindAsync(itemId);
            if (item == null)
            {
                return ApiResponse<bool>.Fail("Checklist item not found.");
            }

            var task = await _dbContext.Tasks.FindAsync(item.TaskId);
            if (task != null)
            {
                var canModifyError = await CheckCanModifyTaskAsync(userId, task);
                if (canModifyError != null)
                {
                    return ApiResponse<bool>.Fail(canModifyError);
                }
            }

            item.IsCompleted = !item.IsCompleted;
            await _dbContext.SaveChangesAsync();
            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<bool>> DeleteChecklistItemAsync(Guid userId, Guid itemId)
        {
            var item = await _dbContext.ChecklistItems.FindAsync(itemId);
            if (item != null)
            {
                var task = await _dbContext.Tasks.FindAsync(item.TaskId);
                if (task != null)
                {
                    var canModifyError = await CheckCanModifyTaskAsync(userId, task);
                    if (canModifyError != null)
                    {
                        return ApiResponse<bool>.Fail(canModifyError);
                    }
                }

                _dbContext.ChecklistItems.Remove(item);
                await _dbContext.SaveChangesAsync();
            }
            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<RecurringTaskRuleViewModel>> SetRecurringTaskRuleAsync(Guid userId, SetRecurringTaskRuleRequestViewModel model)
        {
            var task = await _dbContext.Tasks.FindAsync(model.TaskId);
            if (task == null)
            {
                return ApiResponse<RecurringTaskRuleViewModel>.Fail("Task not found.");
            }

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

            return ApiResponse<RecurringTaskRuleViewModel>.Ok(new RecurringTaskRuleViewModel
            {
                Id = existingRule.Id,
                TaskId = existingRule.TaskId,
                RecurrencePattern = existingRule.RecurrencePattern,
                Interval = existingRule.Interval,
                DaysOfWeek = existingRule.DaysOfWeek,
                NextRunDate = existingRule.NextRunDate,
                IsActive = existingRule.IsActive
            }, "Recurring rule set successfully.");
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

        private static string? ValidateTaskDates(DateTime? startDate, DateTime? dueDate)
        {
            if (startDate.HasValue && dueDate.HasValue && dueDate.Value < startDate.Value)
            {
                return "Task Due Date must be on or after Start Date.";
            }
            return null;
        }

        private async Task<IReadOnlyDictionary<Guid, UserLookup>> GetUserLookupAsync(IEnumerable<Guid> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (!ids.Any()) return new Dictionary<Guid, UserLookup>();

            return await _dbContext.UserLookups
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
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

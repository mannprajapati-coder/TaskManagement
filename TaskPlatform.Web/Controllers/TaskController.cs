using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Collaboration;
using TaskPlatform.Shared.ViewModels.Project;
using TaskPlatform.Shared.ViewModels.Task;

namespace TaskPlatform.Web.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private const long MaxAttachmentSizeBytes = 25 * 1024 * 1024;

        private readonly IApiService _apiService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TaskController(IApiService apiService, IWebHostEnvironment webHostEnvironment)
        {
            _apiService = apiService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? projectId = null, string? status = null, string? priority = null)
        {
            var token = GetAccessToken();

            var myTasksResp = await _apiService.GetMyTasksAsync(null, token);
            var myTasks = myTasksResp.Data ?? new List<TaskViewModel>();

            var projectLookup = new Dictionary<Guid, ProjectViewModel>();
            foreach (var pid in myTasks.Select(t => t.ProjectId).Distinct())
            {
                var projResp = await _apiService.GetProjectByIdAsync(pid.ToString(), token);
                if (projResp.Success && projResp.Data != null)
                {
                    projectLookup[pid] = projResp.Data;
                }
            }

            if (!projectLookup.Any())
            {
                var wsResp = await _apiService.GetMyWorkspacesAsync(token);
                if (wsResp.Success && wsResp.Data != null)
                {
                    foreach (var ws in wsResp.Data)
                    {
                        var wsProjsResp = await _apiService.GetWorkspaceProjectsAsync(ws.Id.ToString(), token);
                        if (wsProjsResp.Success && wsProjsResp.Data != null)
                        {
                            foreach (var p in wsProjsResp.Data)
                            {
                                projectLookup[p.Id] = p;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(projectId) && projectLookup.Any())
            {
                projectId = projectLookup.Keys.First().ToString();
            }

            if (!string.IsNullOrEmpty(projectId) && Guid.TryParse(projectId, out var projGuid) && projectLookup.ContainsKey(projGuid))
            {
                ViewBag.Project = projectLookup[projGuid];
            }
            else if (projectLookup.Any())
            {
                var targetProj = projectLookup.Values.First();
                projectId = targetProj.Id.ToString();
                ViewBag.Project = targetProj;
            }

            ViewBag.ProjectLookup = projectLookup;
            ViewBag.CurrentProjectId = projectId;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPriority = priority;

            if (string.IsNullOrEmpty(projectId))
            {
                return View(new List<TaskViewModel>());
            }

            var tasksResp = await _apiService.GetProjectTasksAsync(projectId, status, priority, token);

            return View(tasksResp.Data ?? new System.Collections.Generic.List<TaskViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> MyTasks(string? projectId = null)
        {
            var token = GetAccessToken();

            var allTasksResp = await _apiService.GetMyTasksAsync(null, token);
            var allTasks = allTasksResp.Data ?? new List<TaskViewModel>();

            var projectLookup = new Dictionary<Guid, ProjectViewModel>();
            foreach (var pid in allTasks.Select(t => t.ProjectId).Distinct())
            {
                var projResp = await _apiService.GetProjectByIdAsync(pid.ToString(), token);
                if (projResp.Success && projResp.Data != null)
                {
                    projectLookup[pid] = projResp.Data;
                }
            }

            var tasks = allTasks;
            if (!string.IsNullOrEmpty(projectId) && Guid.TryParse(projectId, out var projGuid))
            {
                tasks = allTasks.Where(t => t.ProjectId == projGuid).ToList();
            }

            ViewBag.ProjectLookup = projectLookup;
            ViewBag.CurrentProjectId = projectId;

            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> ChecklistHub(string? projectId = null)
        {
            var token = GetAccessToken();

            var allTasksResp = await _apiService.GetMyTasksAsync(null, token);
            var allTasks = allTasksResp.Data ?? new List<TaskViewModel>();

            var projectLookup = new Dictionary<Guid, ProjectViewModel>();
            foreach (var pid in allTasks.Select(t => t.ProjectId).Distinct())
            {
                var projResp = await _apiService.GetProjectByIdAsync(pid.ToString(), token);
                if (projResp.Success && projResp.Data != null)
                {
                    projectLookup[pid] = projResp.Data;
                }
            }

            var tasks = allTasks;
            if (!string.IsNullOrEmpty(projectId) && Guid.TryParse(projectId, out var projGuid))
            {
                var projTasksResp = await _apiService.GetProjectTasksAsync(projectId, null, null, token);
                tasks = projTasksResp.Data ?? new List<TaskViewModel>();
            }

            var taskChecklistMap = new Dictionary<Guid, List<ChecklistItemViewModel>>();
            var subtasksMap = new Dictionary<Guid, List<SubtaskViewModel>>();
            var subtaskChecklistMap = new Dictionary<Guid, List<ChecklistItemViewModel>>();

            foreach (var task in tasks)
            {
                var chkResp = await _apiService.GetChecklistItemsAsync(task.Id.ToString(), token);
                taskChecklistMap[task.Id] = chkResp.Data ?? new List<ChecklistItemViewModel>();

                var subResp = await _apiService.GetSubtasksAsync(task.Id.ToString(), token);
                var subtasks = subResp.Data ?? new List<SubtaskViewModel>();
                subtasksMap[task.Id] = subtasks;

                foreach (var sub in subtasks)
                {
                    var subChkResp = await _apiService.GetChecklistItemsAsync(sub.Id.ToString(), token);
                    subtaskChecklistMap[sub.Id] = subChkResp.Data ?? new List<ChecklistItemViewModel>();
                }
            }

            ViewBag.ProjectLookup = projectLookup;
            ViewBag.CurrentProjectId = projectId;
            ViewBag.TaskChecklistMap = taskChecklistMap;
            ViewBag.SubtasksMap = subtasksMap;
            ViewBag.SubtaskChecklistMap = subtaskChecklistMap;

            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> Gantt(string? projectId = null)
        {
            var token = GetAccessToken();

            var myTasksResp = await _apiService.GetMyTasksAsync(null, token);
            var myTasks = myTasksResp.Data ?? new List<TaskViewModel>();

            var projectLookup = new Dictionary<Guid, ProjectViewModel>();
            foreach (var pid in myTasks.Select(t => t.ProjectId).Distinct())
            {
                var projResp = await _apiService.GetProjectByIdAsync(pid.ToString(), token);
                if (projResp.Success && projResp.Data != null)
                {
                    projectLookup[pid] = projResp.Data;
                }
            }

            if (!projectLookup.Any())
            {
                var wsResp = await _apiService.GetMyWorkspacesAsync(token);
                if (wsResp.Success && wsResp.Data != null)
                {
                    foreach (var ws in wsResp.Data)
                    {
                        var wsProjsResp = await _apiService.GetWorkspaceProjectsAsync(ws.Id.ToString(), token);
                        if (wsProjsResp.Success && wsProjsResp.Data != null)
                        {
                            foreach (var p in wsProjsResp.Data)
                            {
                                projectLookup[p.Id] = p;
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(projectId) && projectLookup.Any())
            {
                projectId = projectLookup.Keys.First().ToString();
            }

            if (!string.IsNullOrEmpty(projectId) && Guid.TryParse(projectId, out var projGuid) && projectLookup.ContainsKey(projGuid))
            {
                ViewBag.Project = projectLookup[projGuid];
            }
            else if (projectLookup.Any())
            {
                var targetProj = projectLookup.Values.First();
                projectId = targetProj.Id.ToString();
                ViewBag.Project = targetProj;
            }

            ViewBag.ProjectLookup = projectLookup;
            ViewBag.CurrentProjectId = projectId;

            if (string.IsNullOrEmpty(projectId))
            {
                return View(new List<TaskViewModel>());
            }

            var tasksResp = await _apiService.GetProjectTasksAsync(projectId, null, null, token);
            return View(tasksResp.Data ?? new System.Collections.Generic.List<TaskViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> Create(string projectId)
        {
            if (!Guid.TryParse(projectId, out var projGuid))
            {
                return RedirectToAction("Index", "Workspace");
            }

            var token = GetAccessToken();
            var membersResp = await _apiService.GetProjectMembersAsync(projectId, token);
            ViewBag.ProjectMembers = membersResp.Data ?? new List<ProjectMemberViewModel>();

            return View(new CreateTaskRequestViewModel { ProjectId = projGuid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskRequestViewModel model)
        {
            var token = GetAccessToken();

            if (!ModelState.IsValid)
            {
                var membersResp = await _apiService.GetProjectMembersAsync(model.ProjectId.ToString(), token);
                ViewBag.ProjectMembers = membersResp.Data ?? new List<ProjectMemberViewModel>();
                return View(model);
            }

            var response = await _apiService.CreateTaskAsync(model, token);

            if (!response.Success || response.Data == null)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                var membersResp = await _apiService.GetProjectMembersAsync(model.ProjectId.ToString(), token);
                ViewBag.ProjectMembers = membersResp.Data ?? new List<ProjectMemberViewModel>();
                return View(model);
            }

            TempData["SuccessMessage"] = response.Message;
            return RedirectToAction(nameof(Detail), new { id = response.Data.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var token = GetAccessToken();
            var response = await _apiService.GetTaskByIdAsync(id, token);

            if (!response.Success || response.Data == null)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction("Index", "Workspace");
            }

            var assigneesResp = await _apiService.GetTaskAssigneesAsync(id, token);
            var membersResp = await _apiService.GetProjectMembersAsync(response.Data.ProjectId.ToString(), token);
            var assignees = assigneesResp.Data ?? new List<TaskAssigneeViewModel>();
            var projectMembers = membersResp.Data ?? new List<ProjectMemberViewModel>();

            if (!await ComputeCanModifyAsync(response.Data.ProjectId, assignees, projectMembers, token))
            {
                TempData["ErrorMessage"] = "Only an assignee, the project owner, or the workspace owner can edit this task.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            ViewBag.ProjectMembers = projectMembers;
            ViewBag.ProjectId = response.Data.ProjectId;

            return View(new UpdateTaskRequestViewModel
            {
                TaskId = response.Data.Id,
                Title = response.Data.Title,
                Description = response.Data.Description,
                Priority = response.Data.Priority,
                StartDate = response.Data.StartDate,
                DueDate = response.Data.DueDate,
                EstimatedHours = response.Data.EstimatedHours,
                ActualHours = response.Data.ActualHours,
                PrimaryAssigneeUserId = response.Data.PrimaryAssigneeUserId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateTaskRequestViewModel model)
        {
            var token = GetAccessToken();

            if (!ModelState.IsValid)
            {
                var taskResp = await _apiService.GetTaskByIdAsync(model.TaskId.ToString(), token);
                var membersResp = await _apiService.GetProjectMembersAsync((taskResp.Data?.ProjectId ?? Guid.Empty).ToString(), token);
                ViewBag.ProjectMembers = membersResp.Data ?? new List<ProjectMemberViewModel>();
                ViewBag.ProjectId = taskResp.Data?.ProjectId;
                return View(model);
            }

            var response = await _apiService.UpdateTaskAsync(model, token);

            if (!response.Success || response.Data == null)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                var taskResp = await _apiService.GetTaskByIdAsync(model.TaskId.ToString(), token);
                var membersResp = await _apiService.GetProjectMembersAsync((taskResp.Data?.ProjectId ?? Guid.Empty).ToString(), token);
                ViewBag.ProjectMembers = membersResp.Data ?? new List<ProjectMemberViewModel>();
                ViewBag.ProjectId = taskResp.Data?.ProjectId;
                return View(model);
            }

            TempData["SuccessMessage"] = "Task updated successfully.";
            return RedirectToAction(nameof(Detail), new { id = response.Data.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            var token = GetAccessToken();
            var response = await _apiService.GetTaskByIdAsync(id, token);

            if (!response.Success || response.Data == null)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction("Index", "Workspace");
            }

            var subtasksResp = await _apiService.GetSubtasksAsync(id, token);
            var assigneesResp = await _apiService.GetTaskAssigneesAsync(id, token);
            var watchersResp = await _apiService.GetTaskWatchersAsync(id, token);
            var checklistResp = await _apiService.GetChecklistItemsAsync(id, token);
            var recurringResp = await _apiService.GetRecurringTaskRuleAsync(id, token);
            var commentsResp = await _apiService.GetTaskCommentsAsync(id, token);
            var attachmentsResp = await _apiService.GetTaskAttachmentsAsync(id, token);
            var membersResp = await _apiService.GetProjectMembersAsync(response.Data.ProjectId.ToString(), token);
            var activityResp = await _apiService.GetTaskActivityAsync(id, token);
            var timeLogsResp = await _apiService.GetTaskTimeLogsAsync(id, token);

            var assignees = assigneesResp.Data ?? new List<TaskAssigneeViewModel>();
            var projectMembers = membersResp.Data ?? new List<ProjectMemberViewModel>();
            var subtasks = subtasksResp.Data ?? new List<SubtaskViewModel>();

            // Merge in each subtask's own activity so the parent's Activity tab shows the full picture,
            // since subtask actions are logged against the subtask's own TaskId, not the parent's.
            var activityLogs = new List<TaskPlatform.Shared.ViewModels.Notification.ActivityLogViewModel>(
                activityResp.Data ?? new List<TaskPlatform.Shared.ViewModels.Notification.ActivityLogViewModel>());

            var subtaskProgress = new Dictionary<Guid, (int Done, int Total)>();
            foreach (var st in subtasks)
            {
                var subtaskActivityResp = await _apiService.GetTaskActivityAsync(st.Id.ToString(), token);
                if (subtaskActivityResp.Data != null)
                {
                    activityLogs.AddRange(subtaskActivityResp.Data);
                }

                var subtaskChecklistResp = await _apiService.GetChecklistItemsAsync(st.Id.ToString(), token);
                var subtaskChecklist = subtaskChecklistResp.Data ?? new List<ChecklistItemViewModel>();
                subtaskProgress[st.Id] = (subtaskChecklist.Count(c => c.IsCompleted), subtaskChecklist.Count);
            }

            activityLogs = activityLogs.OrderByDescending(a => a.Timestamp).ToList();

            ViewBag.Subtasks = subtasks;
            ViewBag.SubtaskTitleLookup = subtasks.ToDictionary(st => st.Id, st => st.Title);
            ViewBag.SubtaskProgress = subtaskProgress;
            ViewBag.Assignees = assignees;
            ViewBag.Watchers = watchersResp.Data ?? new List<TaskWatcherViewModel>();
            ViewBag.ChecklistItems = checklistResp.Data ?? new List<ChecklistItemViewModel>();
            ViewBag.RecurringRule = recurringResp.Success ? recurringResp.Data : null;
            ViewBag.Comments = commentsResp.Data ?? new List<CommentViewModel>();
            ViewBag.Attachments = attachmentsResp.Data ?? new List<AttachmentViewModel>();
            ViewBag.ProjectMembers = projectMembers;
            ViewBag.ActivityLogs = activityLogs;
            ViewBag.TimeLogs = timeLogsResp.Data ?? new List<TaskPlatform.Shared.ViewModels.TimeTracking.TimeLogViewModel>();
            ViewBag.CanModify = await ComputeCanModifyAsync(response.Data.ProjectId, assignees, projectMembers, token);

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubtaskPanel(Guid id, Guid projectId)
        {
            var token = GetAccessToken();

            var subtaskResp = await _apiService.GetTaskByIdAsync(id.ToString(), token);
            if (!subtaskResp.Success || subtaskResp.Data == null)
            {
                return NotFound();
            }

            var checklistResp = await _apiService.GetChecklistItemsAsync(id.ToString(), token);
            var assigneesResp = await _apiService.GetTaskAssigneesAsync(id.ToString(), token);
            var membersResp = await _apiService.GetProjectMembersAsync(projectId.ToString(), token);
            var activityResp = await _apiService.GetTaskActivityAsync(id.ToString(), token);

            var assignees = assigneesResp.Data ?? new List<TaskAssigneeViewModel>();
            var projectMembers = membersResp.Data ?? new List<ProjectMemberViewModel>();

            ViewBag.ChecklistItems = checklistResp.Data ?? new List<ChecklistItemViewModel>();
            ViewBag.Assignees = assignees;
            ViewBag.ProjectMembers = projectMembers;
            ViewBag.ActivityLogs = activityResp.Data ?? new List<TaskPlatform.Shared.ViewModels.Notification.ActivityLogViewModel>();
            ViewBag.CanModify = await ComputeCanModifyAsync(projectId, assignees, projectMembers, token);

            return PartialView("_SubtaskPanel", subtaskResp.Data);
        }

        private async Task<bool> ComputeCanModifyAsync(Guid projectId, List<TaskAssigneeViewModel> assignees, List<ProjectMemberViewModel> projectMembers, string token)
        {
            var currentUserIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(currentUserIdClaim, out var currentUserId))
            {
                return false;
            }

            if (assignees.Any(a => a.UserId == currentUserId))
            {
                return true;
            }

            if (projectMembers.Any(m => m.UserId == currentUserId && m.Role == "Owner"))
            {
                return true;
            }

            var projectResp = await _apiService.GetProjectByIdAsync(projectId.ToString(), token);
            if (projectResp.Success && projectResp.Data != null)
            {
                var workspaceResp = await _apiService.GetWorkspaceByIdAsync(projectResp.Data.WorkspaceId.ToString(), token);
                if (workspaceResp.Success && workspaceResp.Data != null && workspaceResp.Data.OwnerUserId == currentUserId)
                {
                    return true;
                }
            }

            return false;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(UpdateTaskStatusRequestViewModel model, string projectId)
        {
            var token = GetAccessToken();
            var response = await _apiService.UpdateTaskStatusAsync(model, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = model.TaskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatusAjax([FromBody] UpdateTaskStatusRequestViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.UpdateTaskStatusAsync(model, token);
            return Json(new { success = response.Success, message = response.Message, status = response.Data?.Status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder([FromBody] ReorderTasksRequestViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.ReorderTasksAsync(model, token);
            return Json(new { success = response.Success, message = response.Message });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubtaskAjax(Guid id)
        {
            var token = GetAccessToken();
            var response = await _apiService.DeleteTaskAsync(id.ToString(), token);
            return Json(new { success = response.Success, message = response.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubtask(CreateSubtaskRequestViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.CreateSubtaskAsync(model, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = model.ParentTaskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleWatcher(string id)
        {
            var token = GetAccessToken();
            var response = await _apiService.ToggleTaskWatcherAsync(id, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id, string projectId)
        {
            var token = GetAccessToken();
            var response = await _apiService.DeleteTaskWithSubtasksAsync(id, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Index), new { projectId = projectId });
        }

        // Checklist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddChecklistItem(AddChecklistItemRequestViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.AddChecklistItemAsync(model, token);

            if (!response.Success)
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = model.TaskId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleChecklistItemAjax(Guid id)
        {
            var token = GetAccessToken();
            var response = await _apiService.ToggleChecklistItemAsync(id.ToString(), token);
            return Json(new { success = response.Success, message = response.Message });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteChecklistItemAjax(Guid id)
        {
            var token = GetAccessToken();
            var response = await _apiService.DeleteChecklistItemAsync(id.ToString(), token);
            return Json(new { success = response.Success, message = response.Message });
        }

        // Recurring Rule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetRecurringRule(SetRecurringTaskRuleRequestViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.SetRecurringTaskRuleAsync(model, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = "Recurring rule saved.";
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = model.TaskId });
        }

        // Assignees
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssignee(AddTaskAssigneeRequestViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.AddTaskAssigneeAsync(model, token);

            if (!response.Success)
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = model.TaskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssignee(Guid taskId, Guid targetUserId)
        {
            var token = GetAccessToken();
            var response = await _apiService.RemoveTaskAssigneeAsync(taskId.ToString(), targetUserId.ToString(), token);

            if (!response.Success)
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = taskId });
        }

        // Comments
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(AddCommentRequestViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.AddTaskCommentAsync(model, token);

            if (!response.Success)
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = model.TaskId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(Guid id, Guid taskId)
        {
            var token = GetAccessToken();
            var response = await _apiService.DeleteTaskCommentAsync(id.ToString(), token);

            if (!response.Success)
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = taskId });
        }

        // Attachments
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(Guid taskId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please choose a file to upload.";
                return RedirectToAction(nameof(Detail), new { id = taskId });
            }

            if (file.Length > MaxAttachmentSizeBytes)
            {
                TempData["ErrorMessage"] = "File is too large. Maximum size is 25 MB.";
                return RedirectToAction(nameof(Detail), new { id = taskId });
            }

            var storedFileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var relativeDir = Path.Combine("uploads", taskId.ToString());
            var absoluteDir = Path.Combine(_webHostEnvironment.WebRootPath, relativeDir);
            Directory.CreateDirectory(absoluteDir);

            var absolutePath = Path.Combine(absoluteDir, storedFileName);
            using (var stream = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = "/" + Path.Combine(relativeDir, storedFileName).Replace('\\', '/');

            var token = GetAccessToken();
            var response = await _apiService.AddTaskAttachmentAsync(
                taskId.ToString(), file.FileName, relativePath, file.Length,
                string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType, token);

            if (!response.Success)
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = taskId });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAttachment(Guid id, Guid taskId)
        {
            var token = GetAccessToken();
            var attachmentsResp = await _apiService.GetTaskAttachmentsAsync(taskId.ToString(), token);
            var attachment = attachmentsResp.Data?.Find(a => a.Id == id);

            if (attachment == null)
            {
                TempData["ErrorMessage"] = "Attachment not found.";
                return RedirectToAction(nameof(Detail), new { id = taskId });
            }

            var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(physicalPath))
            {
                TempData["ErrorMessage"] = "The file could not be found on disk.";
                return RedirectToAction(nameof(Detail), new { id = taskId });
            }

            return PhysicalFile(physicalPath, attachment.ContentType, attachment.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> PreviewAttachment(Guid id, Guid taskId)
        {
            var token = GetAccessToken();
            var attachmentsResp = await _apiService.GetTaskAttachmentsAsync(taskId.ToString(), token);
            var attachment = attachmentsResp.Data?.Find(a => a.Id == id);

            if (attachment == null)
            {
                return NotFound("Attachment not found.");
            }

            var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound("File not found on disk.");
            }

            Response.Headers.Append("Content-Disposition", $"inline; filename=\"{attachment.FileName}\"");
            return PhysicalFile(physicalPath, string.IsNullOrEmpty(attachment.ContentType) ? "application/octet-stream" : attachment.ContentType);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(Guid id, Guid taskId)
        {
            var token = GetAccessToken();
            var attachmentsResp = await _apiService.GetTaskAttachmentsAsync(taskId.ToString(), token);
            var attachment = attachmentsResp.Data?.Find(a => a.Id == id);

            var response = await _apiService.DeleteTaskAttachmentAsync(id.ToString(), token);

            if (response.Success && attachment != null)
            {
                var physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, attachment.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                try
                {
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }
                catch
                {
                    // Best-effort cleanup; the DB row (source of truth) is already removed.
                }
            }
            else if (!response.Success)
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Detail), new { id = taskId });
        }

        private string GetAccessToken() => User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

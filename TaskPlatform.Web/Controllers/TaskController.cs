using System;
using System.Collections.Generic;
using System.IO;
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
        public async Task<IActionResult> Index(string projectId, string? status = null, string? priority = null)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                return RedirectToAction("Index", "Workspace");
            }

            var token = GetAccessToken();
            var projectResp = await _apiService.GetProjectByIdAsync(projectId, token);
            if (!projectResp.Success || projectResp.Data == null)
            {
                return RedirectToAction("Index", "Workspace");
            }

            ViewBag.Project = projectResp.Data;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPriority = priority;

            var tasksResp = await _apiService.GetProjectTasksAsync(projectId, status, priority, token);

            return View(tasksResp.Data ?? new System.Collections.Generic.List<TaskViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> Gantt(string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                return RedirectToAction("Index", "Workspace");
            }

            var token = GetAccessToken();
            var projectResp = await _apiService.GetProjectByIdAsync(projectId, token);
            if (!projectResp.Success || projectResp.Data == null)
            {
                return RedirectToAction("Index", "Workspace");
            }

            ViewBag.Project = projectResp.Data;
            var tasksResp = await _apiService.GetProjectTasksAsync(projectId, null, null, token);
            return View(tasksResp.Data ?? new System.Collections.Generic.List<TaskViewModel>());
        }

        [HttpGet]
        public IActionResult Create(string projectId)
        {
            if (!Guid.TryParse(projectId, out var projGuid))
            {
                return RedirectToAction("Index", "Workspace");
            }

            return View(new CreateTaskRequestViewModel { ProjectId = projGuid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = GetAccessToken();
            var response = await _apiService.CreateTaskAsync(model, token);

            if (!response.Success || response.Data == null)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = response.Message;
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

            ViewBag.Subtasks = subtasksResp.Data ?? new List<SubtaskViewModel>();
            ViewBag.Assignees = assigneesResp.Data ?? new List<TaskAssigneeViewModel>();
            ViewBag.Watchers = watchersResp.Data ?? new List<TaskWatcherViewModel>();
            ViewBag.ChecklistItems = checklistResp.Data ?? new List<ChecklistItemViewModel>();
            ViewBag.RecurringRule = recurringResp.Success ? recurringResp.Data : null;
            ViewBag.Comments = commentsResp.Data ?? new List<CommentViewModel>();
            ViewBag.Attachments = attachmentsResp.Data ?? new List<AttachmentViewModel>();
            ViewBag.ProjectMembers = membersResp.Data ?? new List<ProjectMemberViewModel>();

            return View(response.Data);
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

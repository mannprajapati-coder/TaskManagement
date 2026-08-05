using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Task;

namespace TaskPlatform.Web.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly IApiService _apiService;

        public TaskController(IApiService apiService)
        {
            _apiService = apiService;
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

            ViewBag.Subtasks = subtasksResp.Data ?? new System.Collections.Generic.List<SubtaskViewModel>();
            ViewBag.Assignees = assigneesResp.Data ?? new System.Collections.Generic.List<TaskAssigneeViewModel>();
            ViewBag.Watchers = watchersResp.Data ?? new System.Collections.Generic.List<TaskWatcherViewModel>();

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

        private string GetAccessToken() => User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

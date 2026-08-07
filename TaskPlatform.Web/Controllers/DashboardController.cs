using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Dashboard;
using TaskPlatform.Shared.ViewModels.Project;
using TaskPlatform.Shared.ViewModels.Task;
using TaskPlatform.Web.Helpers;

namespace TaskPlatform.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IApiService _apiService;

        public DashboardController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? workspaceId = null)
        {
            try
            {
                var token = GetAccessToken();

                var workspacesResp = await _apiService.GetMyWorkspacesAsync(token);
                if (workspacesResp == null || !workspacesResp.Success || workspacesResp.Data == null || !workspacesResp.Data.Any())
                {
                    if (workspacesResp != null && !workspacesResp.Success && (workspacesResp.Message.Contains("401") || workspacesResp.Message.Contains("Unauthorized")))
                    {
                        TempData["ErrorMessage"] = "Your session expired. Please log in again.";
                        return RedirectToAction("Login", "Auth");
                    }
                    TempData["SuccessMessage"] = "Welcome! Create your first workspace to get started.";
                    return RedirectToAction("Create", "Workspace");
                }

                ViewBag.Workspaces = workspacesResp.Data;

                var targetWorkspace = !string.IsNullOrEmpty(workspaceId)
                    ? workspacesResp.Data.FirstOrDefault(w => w.Id.ToString() == workspaceId)
                    : (WorkspaceCookie.Get(HttpContext) is Guid cookieWorkspaceId
                        ? workspacesResp.Data.FirstOrDefault(w => w.Id == cookieWorkspaceId)
                        : null);
                targetWorkspace ??= workspacesResp.Data.FirstOrDefault();

                if (targetWorkspace == null)
                {
                    return RedirectToAction("Create", "Workspace");
                }

                WorkspaceCookie.Set(HttpContext, targetWorkspace.Id);
                ViewBag.CurrentWorkspace = targetWorkspace;

                // Load Dashboard Overview Data
                var dashboardResp = await _apiService.GetDashboardOverviewAsync(targetWorkspace.Id.ToString(), token);
                var model = (dashboardResp != null && dashboardResp.Data != null) ? dashboardResp.Data : new DashboardOverviewViewModel
                {
                    WorkspaceId = targetWorkspace.Id,
                    TotalProjects = 0,
                    TotalTasks = 0,
                    CompletedTasks = 0,
                    InProgressTasks = 0,
                    PendingTasks = 0,
                    OverdueTasks = 0,
                    CompletionRatePercentage = 0
                };

                // Enrich ViewBag with MyTasks and Projects
                var myTasksResp = await _apiService.GetMyTasksAsync(null,token);
                ViewBag.MyTasks = myTasksResp?.Data?.Take(5).ToList() ?? new List<TaskViewModel>();

                var projectsResp = await _apiService.GetWorkspaceProjectsAsync(targetWorkspace.Id.ToString(), token);
                ViewBag.Projects = projectsResp?.Data ?? new List<ProjectViewModel>();

                return View(model);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred loading the dashboard. Please try again.";
                return RedirectToAction("Create", "Workspace");
            }
        }

        private string GetAccessToken() => User.Claims.FirstOrDefault(c => c.Type == "AccessToken")?.Value ?? User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

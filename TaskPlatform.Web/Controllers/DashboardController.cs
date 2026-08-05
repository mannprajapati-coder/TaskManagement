using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Dashboard;

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
            var token = GetAccessToken();

            var workspacesResp = await _apiService.GetMyWorkspacesAsync(token);
            if (!workspacesResp.Success || workspacesResp.Data == null || !workspacesResp.Data.Any())
            {
                if (!workspacesResp.Success && (workspacesResp.Message.Contains("401") || workspacesResp.Message.Contains("Unauthorized")))
                {
                    TempData["ErrorMessage"] = "Your session expired. Please log in again.";
                    return RedirectToAction("Login", "Auth");
                }
                return RedirectToAction("Create", "Workspace");
            }

            ViewBag.Workspaces = workspacesResp.Data;

            var targetWorkspace = string.IsNullOrEmpty(workspaceId)
                ? workspacesResp.Data.FirstOrDefault()
                : workspacesResp.Data.FirstOrDefault(w => w.Id.ToString() == workspaceId) ?? workspacesResp.Data.FirstOrDefault();

            if (targetWorkspace == null)
            {
                return RedirectToAction("Create", "Workspace");
            }

            ViewBag.CurrentWorkspace = targetWorkspace;

            var dashboardResp = await _apiService.GetDashboardOverviewAsync(targetWorkspace.Id.ToString(), token);

            var model = dashboardResp.Data ?? new DashboardOverviewViewModel
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

            return View(model);
        }

        private string GetAccessToken() => User.Claims.FirstOrDefault(c => c.Type == "AccessToken")?.Value ?? User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

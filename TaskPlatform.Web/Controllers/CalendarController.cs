using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Calendar;

namespace TaskPlatform.Web.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly IApiService _apiService;

        public CalendarController(IApiService apiService)
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

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Events(string workspaceId, DateTime start, DateTime end)
        {
            var token = GetAccessToken();
            var response = await _apiService.GetCalendarEventsAsync(workspaceId, start, end, token);
            return Json(response.Data ?? new System.Collections.Generic.List<CalendarEventViewModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule([FromBody] RescheduleTaskDateRequestViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.RescheduleTaskAsync(model, token);
            return Json(new { success = response.Success, message = response.Message });
        }

        private string GetAccessToken() => User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.TimeTracking;

namespace TaskPlatform.Web.Controllers
{
    [Authorize]
    public class TimeTrackingController : Controller
    {
        private readonly IApiService _apiService;

        public TimeTrackingController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTimer(Guid taskId)
        {
            var token = GetAccessToken();
            var response = await _apiService.StartTimerAsync(taskId.ToString(), token);
            return Json(new { success = response.Success, message = response.Message, data = response.Data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopTimer(string? notes)
        {
            var token = GetAccessToken();
            var response = await _apiService.StopTimerAsync(notes, token);
            return Json(new { success = response.Success, message = response.Message, data = response.Data });
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveTimer()
        {
            var token = GetAccessToken();
            var response = await _apiService.GetActiveTimerAsync(token);
            return Json(new { success = response.Success, data = response.Data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNotes(Guid timeLogId, string? notes)
        {
            var token = GetAccessToken();
            var response = await _apiService.UpdateTimeLogNotesAsync(timeLogId.ToString(), notes, token);
            return Json(new { success = response.Success, message = response.Message, data = response.Data });
        }

        [HttpGet]
        public async Task<IActionResult> Report(string? workspaceId, string range = "today", DateTime? from = null, DateTime? to = null)
        {
            var token = GetAccessToken();

            var workspacesResp = await _apiService.GetMyWorkspacesAsync(token);
            if (workspacesResp == null || !workspacesResp.Success || workspacesResp.Data == null || !workspacesResp.Data.Any())
            {
                TempData["ErrorMessage"] = "You need a workspace to view time tracking reports.";
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Workspaces = workspacesResp.Data;

            var currentWorkspace = string.IsNullOrEmpty(workspaceId)
                ? workspacesResp.Data.FirstOrDefault()
                : workspacesResp.Data.FirstOrDefault(w => w.Id.ToString() == workspaceId) ?? workspacesResp.Data.FirstOrDefault();

            if (currentWorkspace == null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.CurrentWorkspace = currentWorkspace;
            ViewBag.Range = range;

            var (rangeFrom, rangeTo) = ResolveDateRange(range, from, to);
            ViewBag.From = rangeFrom;
            ViewBag.To = rangeTo;

            var reportResp = await _apiService.GetTimeTrackingReportAsync(currentWorkspace.Id.ToString(), rangeFrom, rangeTo, token);

            if (!reportResp.Success)
            {
                TempData["ErrorMessage"] = reportResp.Message;
                return RedirectToAction("Index", "Dashboard");
            }

            return View(reportResp.Data ?? new TimeTrackingReportViewModel());
        }

        private static (DateTime from, DateTime to) ResolveDateRange(string range, DateTime? customFrom, DateTime? customTo)
        {
            var today = DateTime.UtcNow.Date;
            switch (range)
            {
                case "week":
                    var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                    return (startOfWeek, startOfWeek.AddDays(7).AddTicks(-1));
                case "month":
                    var startOfMonth = new DateTime(today.Year, today.Month, 1);
                    return (startOfMonth, startOfMonth.AddMonths(1).AddTicks(-1));
                case "custom":
                    var from = (customFrom ?? today).Date;
                    var to = (customTo ?? today).Date.AddDays(1).AddTicks(-1);
                    return (from, to);
                default:
                    return (today, today.AddDays(1).AddTicks(-1));
            }
        }

        private string GetAccessToken() => User.Claims.FirstOrDefault(c => c.Type == "AccessToken")?.Value ?? User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

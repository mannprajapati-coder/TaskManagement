using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Notification;

namespace TaskPlatform.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly IApiService _apiService;

        public NotificationsController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = GetAccessToken();
            var response = await _apiService.GetMyNotificationsAsync(false, token);
            return View(response.Data ?? new List<NotificationItemViewModel>());
        }

        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var token = GetAccessToken();
            var response = await _apiService.GetMyNotificationsAsync(true, token);
            return Json(response.Data ?? new List<NotificationItemViewModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(System.Guid id)
        {
            var token = GetAccessToken();
            var response = await _apiService.MarkNotificationAsReadAsync(id.ToString(), token);
            return Json(new { success = response.Success, message = response.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var token = GetAccessToken();
            var response = await _apiService.MarkAllNotificationsAsReadAsync(token);
            return Json(new { success = response.Success, message = response.Message });
        }

        private string GetAccessToken() => User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.User;

namespace TaskPlatform.Web.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IApiService _apiService;

        public UsersController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var token = GetAccessToken();
            var response = await _apiService.GetMyProfileAsync(token);

            if (!response.Success || response.Data == null)
            {
                TempData["ErrorMessage"] = response.Message;
                return View(new UserProfileViewModel());
            }

            return View(response.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Profile));
            }

            var token = GetAccessToken();
            var response = await _apiService.UpdateProfileAsync(model, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Password validation failed.";
                return RedirectToAction(nameof(Profile));
            }

            var token = GetAccessToken();
            var response = await _apiService.ChangePasswordAsync(model, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> Preferences()
        {
            var token = GetAccessToken();
            var response = await _apiService.GetMyPreferencesAsync(token);

            return View(response.Data ?? new UserPreferenceViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preferences(UserPreferenceViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.UpdatePreferencesAsync(model, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Preferences));
        }

        [HttpGet]
        public async Task<IActionResult> Sessions()
        {
            var token = GetAccessToken();
            var response = await _apiService.GetMyActiveSessionsAsync(token);

            return View(response.Data ?? new System.Collections.Generic.List<ActiveSessionViewModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeSession(string sessionId)
        {
            var token = GetAccessToken();
            var response = await _apiService.RevokeSessionAsync(sessionId, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Sessions));
        }

        private string GetAccessToken() => User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

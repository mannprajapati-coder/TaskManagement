using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Workspace;

namespace TaskPlatform.Web.Controllers
{
    [Authorize]
    public class WorkspaceController : Controller
    {
        private readonly IApiService _apiService;

        public WorkspaceController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = GetAccessToken();
            var response = await _apiService.GetMyWorkspacesAsync(token);
            return View(response.Data ?? new System.Collections.Generic.List<WorkspaceViewModel>());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateWorkspaceRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateWorkspaceRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = GetAccessToken();
            var response = await _apiService.CreateWorkspaceAsync(model, token);

            if (!response.Success || response.Data == null)
            {
                if (response.Message.Contains("401") || response.Message.Contains("Unauthorized"))
                {
                    TempData["ErrorMessage"] = "Your session expired. Please log in again.";
                    return RedirectToAction("Login", "Auth");
                }
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = response.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var token = GetAccessToken();
            var response = await _apiService.GetWorkspaceByIdAsync(id, token);

            if (!response.Success || response.Data == null)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Settings(string id)
        {
            var token = GetAccessToken();
            var response = await _apiService.GetWorkspaceByIdAsync(id, token);

            if (!response.Success || response.Data == null)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            return View(response.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(UpdateWorkspaceSettingsRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Settings", new WorkspaceViewModel { Id = model.WorkspaceId, Name = model.Name, Description = model.Description });
            }

            var token = GetAccessToken();
            var response = await _apiService.UpdateWorkspaceSettingsAsync(model, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Settings), new { id = model.WorkspaceId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Archive(string id)
        {
            var token = GetAccessToken();
            var response = await _apiService.ArchiveWorkspaceAsync(id, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteMembers(InviteMembersRequestViewModel model)
        {
            var token = GetAccessToken();
            var response = await _apiService.InviteMembersAsync(model, token);

            if (response.Success && response.Data != null)
            {
                TempData["InviteUrl"] = response.Data.InviteUrl;
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Settings), new { id = model.WorkspaceId });
        }

        [HttpGet]
        public IActionResult Join(string? token = null)
        {
            return View(new JoinWorkspaceRequestViewModel { Token = token ?? string.Empty });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(JoinWorkspaceRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var accessToken = GetAccessToken();
            var response = await _apiService.JoinViaInviteAsync(model.Token, accessToken);

            if (!response.Success)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Joined workspace successfully!";
            return RedirectToAction(nameof(Index));
        }

        private string GetAccessToken() => User.Claims.FirstOrDefault(c => c.Type == "AccessToken")?.Value ?? User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

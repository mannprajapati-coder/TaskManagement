using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Project;

namespace TaskPlatform.Web.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly IApiService _apiService;

        public ProjectController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string workspaceId)
        {
            if (string.IsNullOrEmpty(workspaceId))
            {
                return RedirectToAction("Index", "Workspace");
            }

            ViewBag.WorkspaceId = workspaceId;
            var token = GetAccessToken();
            var response = await _apiService.GetWorkspaceProjectsAsync(workspaceId, token);

            return View(response.Data ?? new System.Collections.Generic.List<ProjectViewModel>());
        }

        [HttpGet]
        public IActionResult Create(string workspaceId)
        {
            if (!Guid.TryParse(workspaceId, out var wsGuid))
            {
                return RedirectToAction("Index", "Workspace");
            }

            return View(new CreateProjectRequestViewModel { WorkspaceId = wsGuid });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = GetAccessToken();
            var response = await _apiService.CreateProjectAsync(model, token);

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
            var response = await _apiService.GetProjectByIdAsync(id, token);

            if (!response.Success || response.Data == null)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction("Index", "Workspace");
            }

            return View(response.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(string id, string workspaceId)
        {
            var token = GetAccessToken();
            await _apiService.ToggleFavoriteProjectAsync(id, token);
            return RedirectToAction(nameof(Index), new { workspaceId = workspaceId });
        }

        [HttpGet]
        public async Task<IActionResult> Members(string id)
        {
            var token = GetAccessToken();
            var projectResp = await _apiService.GetProjectByIdAsync(id, token);
            if (!projectResp.Success || projectResp.Data == null)
            {
                return RedirectToAction("Index", "Workspace");
            }

            ViewBag.Project = projectResp.Data;

            var membersResp = await _apiService.GetProjectMembersAsync(id, token);
            var joinReqsResp = await _apiService.GetPendingProjectJoinRequestsAsync(id, token);

            ViewBag.JoinRequests = joinReqsResp.Data ?? new System.Collections.Generic.List<ProjectJoinRequestViewModel>();

            return View(membersResp.Data ?? new System.Collections.Generic.List<ProjectMemberViewModel>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveJoinRequest(ResolveJoinRequestViewModel model, string projectId)
        {
            var token = GetAccessToken();
            var response = await _apiService.ResolveProjectJoinRequestAsync(model, token);

            if (response.Success)
            {
                TempData["SuccessMessage"] = response.Message;
            }
            else
            {
                TempData["ErrorMessage"] = response.Message;
            }

            return RedirectToAction(nameof(Members), new { id = projectId });
        }

        private string GetAccessToken() => User.FindFirst("AccessToken")?.Value ?? string.Empty;
    }
}

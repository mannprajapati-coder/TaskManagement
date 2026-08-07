using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Workspace;
using TaskPlatform.Web.Helpers;

namespace TaskPlatform.Web.ViewComponents
{
    public class WorkspaceSwitcherViewComponent : ViewComponent
    {
        private readonly IApiService _apiService;

        public WorkspaceSwitcherViewComponent(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var token = HttpContext.User.FindFirst("AccessToken")?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(token))
            {
                return View(new WorkspaceSwitcherViewModel());
            }

            var response = await _apiService.GetMyWorkspacesAsync(token);
            var workspaces = response?.Data ?? new System.Collections.Generic.List<WorkspaceViewModel>();

            var currentId = WorkspaceCookie.Get(HttpContext) ?? workspaces.FirstOrDefault()?.Id;

            return View(new WorkspaceSwitcherViewModel
            {
                Workspaces = workspaces,
                CurrentWorkspaceId = currentId
            });
        }
    }

    public class WorkspaceSwitcherViewModel
    {
        public System.Collections.Generic.List<WorkspaceViewModel> Workspaces { get; set; } = new();
        public System.Guid? CurrentWorkspaceId { get; set; }
    }
}

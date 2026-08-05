using System;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Dashboard;

namespace Modules.Tasks.Domain.IServices
{
    public interface IDashboardService
    {
        Task<DashboardOverviewViewModel> GetWorkspaceDashboardOverviewAsync(Guid workspaceId, Guid userId);
    }
}

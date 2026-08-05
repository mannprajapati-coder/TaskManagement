using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.User;

namespace Modules.UserManagement.Domain.IServices
{
    public interface IUserService
    {
        Task<UserProfileViewModel> GetProfileAsync(Guid userId);
        Task<UserProfileViewModel> UpdateProfileAsync(Guid userId, UpdateProfileRequestViewModel model);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequestViewModel model);
        Task<UserPreferenceViewModel> GetPreferencesAsync(Guid userId);
        Task<UserPreferenceViewModel> UpdatePreferencesAsync(Guid userId, UserPreferenceViewModel model);
        Task<List<ActiveSessionViewModel>> GetActiveSessionsAsync(Guid userId, string? currentTokenHash = null);
        Task<bool> RevokeSessionAsync(Guid userId, Guid sessionId);
    }
}

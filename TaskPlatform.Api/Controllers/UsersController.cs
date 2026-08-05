using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Modules.UserManagement.Domain.IServices;
using TaskPlatform.Shared.ViewModels.Common;
using TaskPlatform.Shared.ViewModels.User;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("GetMyProfile")]
        public async Task<ActionResult<ApiResponse<UserProfileViewModel>>> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.GetProfileAsync(userId);
            return Ok(ApiResponse<UserProfileViewModel>.Ok(result));
        }

        [HttpPut("UpdateProfile")]
        public async Task<ActionResult<ApiResponse<UserProfileViewModel>>> UpdateProfile([FromBody] UpdateProfileRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _userService.UpdateProfileAsync(userId, model);
            return Ok(ApiResponse<UserProfileViewModel>.Ok(result, "Profile updated successfully."));
        }

        [HttpPut("ChangePassword")]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _userService.ChangePasswordAsync(userId, model);
            return Ok(ApiResponse<bool>.Ok(result, "Password changed successfully."));
        }

        [HttpGet("GetMyPreferences")]
        public async Task<ActionResult<ApiResponse<UserPreferenceViewModel>>> GetMyPreferences()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.GetPreferencesAsync(userId);
            return Ok(ApiResponse<UserPreferenceViewModel>.Ok(result));
        }

        [HttpPut("UpdatePreferences")]
        public async Task<ActionResult<ApiResponse<UserPreferenceViewModel>>> UpdatePreferences([FromBody] UserPreferenceViewModel model)
        {
            var userId = GetCurrentUserId();
            var result = await _userService.UpdatePreferencesAsync(userId, model);
            return Ok(ApiResponse<UserPreferenceViewModel>.Ok(result, "Preferences updated successfully."));
        }

        [HttpGet("GetMyActiveSessions")]
        public async Task<ActionResult<ApiResponse<List<ActiveSessionViewModel>>>> GetMyActiveSessions()
        {
            var userId = GetCurrentUserId();
            var result = await _userService.GetActiveSessionsAsync(userId);
            return Ok(ApiResponse<List<ActiveSessionViewModel>>.Ok(result));
        }

        [HttpDelete("RevokeSession/{sessionId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RevokeSession(Guid sessionId)
        {
            var userId = GetCurrentUserId();
            var result = await _userService.RevokeSessionAsync(userId, sessionId);
            return Ok(ApiResponse<bool>.Ok(result, "Session revoked successfully."));
        }

        private Guid GetCurrentUserId()
        {
            var subClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated.");
            }
            return userId;
        }
    }
}

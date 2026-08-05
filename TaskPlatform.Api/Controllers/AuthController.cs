using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modules.Authentication.Domain.IServices;
using TaskPlatform.Shared.ViewModels.Auth;
using TaskPlatform.Shared.ViewModels.Common;

namespace TaskPlatform.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("Register")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<ActionResult<ApiResponse<AuthResponseViewModel>>> Register([FromBody] RegisterRequestViewModel model)
        {
            var result = await _authService.RegisterAsync(model);
            return Ok(ApiResponse<AuthResponseViewModel>.Ok(result, "Registration successful. Please check your email for verification."));
        }

        [HttpPost("VerifyEmail")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> VerifyEmail([FromBody] VerifyEmailRequestViewModel model)
        {
            var result = await _authService.VerifyEmailAsync(model);
            return Ok(ApiResponse<bool>.Ok(result, "Email verified successfully. You can now log in."));
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<ActionResult<ApiResponse<AuthResponseViewModel>>> Login([FromBody] LoginRequestViewModel model)
        {
            var result = await _authService.LoginAsync(model);
            return Ok(ApiResponse<AuthResponseViewModel>.Ok(result, "Login successful."));
        }

        [HttpPost("RefreshToken")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponseViewModel>>> RefreshToken([FromBody] RefreshTokenRequestViewModel model)
        {
            var result = await _authService.RefreshTokenAsync(model.RefreshToken);
            return Ok(ApiResponse<AuthResponseViewModel>.Ok(result, "Token refreshed successfully."));
        }

        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword([FromBody] ForgotPasswordRequestViewModel model)
        {
            var result = await _authService.ForgotPasswordAsync(model.Email);
            return Ok(ApiResponse<bool>.Ok(result, "If the account exists, a password reset link has been sent to your email."));
        }

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> ResetPassword([FromBody] ResetPasswordRequestViewModel model)
        {
            var result = await _authService.ResetPasswordAsync(model);
            return Ok(ApiResponse<bool>.Ok(result, "Password reset successful. Please log in with your new password."));
        }

        [HttpPost("GoogleLogin")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponseViewModel>>> GoogleLogin([FromBody] GoogleLoginRequestViewModel model)
        {
            var result = await _authService.GoogleLoginAsync(model.IdToken);
            return Ok(ApiResponse<AuthResponseViewModel>.Ok(result, "Google login successful."));
        }

        [HttpPost("EnableMfa")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<MfaSetupResponseViewModel>>> EnableMfa()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<MfaSetupResponseViewModel>.Fail("Unauthorized"));
            }

            var result = await _authService.EnableMfaAsync(userId);
            return Ok(ApiResponse<MfaSetupResponseViewModel>.Ok(result, "MFA secret generated successfully."));
        }

        [HttpPost("VerifyMfa")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponseViewModel>>> VerifyMfa([FromBody] VerifyMfaRequestViewModel model)
        {
            var result = await _authService.VerifyMfaAsync(model);
            return Ok(ApiResponse<AuthResponseViewModel>.Ok(result, "MFA code verified successfully."));
        }

        [HttpPost("Logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> Logout([FromBody] RefreshTokenRequestViewModel model)
        {
            var result = await _authService.LogoutAsync(model.RefreshToken);
            return Ok(ApiResponse<bool>.Ok(result, "Logged out successfully."));
        }
    }
}

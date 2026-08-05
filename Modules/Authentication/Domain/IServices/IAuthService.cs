using System;
using System.Threading.Tasks;
using TaskPlatform.Shared.ViewModels.Auth;

namespace Modules.Authentication.Domain.IServices
{
    public interface IAuthService
    {
        Task<AuthResponseViewModel> RegisterAsync(RegisterRequestViewModel model);
        Task<bool> VerifyEmailAsync(VerifyEmailRequestViewModel model);
        Task<AuthResponseViewModel> LoginAsync(LoginRequestViewModel model);
        Task<AuthResponseViewModel> RefreshTokenAsync(string presentedRefreshToken);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordRequestViewModel model);
        Task<AuthResponseViewModel> GoogleLoginAsync(string idToken);
        Task<MfaSetupResponseViewModel> EnableMfaAsync(Guid userId);
        Task<AuthResponseViewModel> VerifyMfaAsync(VerifyMfaRequestViewModel model);
        Task<bool> LogoutAsync(string presentedRefreshToken);
    }
}

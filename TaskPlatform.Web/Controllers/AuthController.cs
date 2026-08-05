using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlatform.Shared.ApiService;
using TaskPlatform.Shared.ViewModels.Auth;

namespace TaskPlatform.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IApiService _apiService;

        public AuthController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (User.Identity?.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            return View(new LoginRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginRequestViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.LoginAsync(model);
            if (!response.Success || response.Data == null)
            {
                ModelState.AddModelError(string.Empty, response.Message ?? "Invalid login attempt.");
                return View(model);
            }

            if (response.Data.MfaRequired)
            {
                TempData["MfaChallengeToken"] = response.Data.MfaChallengeToken;
                return RedirectToAction(nameof(VerifyMfa));
            }

            await SignInUserAsync(response.Data);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.RegisterAsync(model);
            if (!response.Success)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = response.Message;
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string userId, string token)
        {
            var model = new VerifyEmailRequestViewModel { UserId = userId, Token = token };
            var response = await _apiService.VerifyEmailAsync(model);

            ViewBag.Success = response.Success;
            ViewBag.Message = response.Message;
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.ForgotPasswordAsync(model);
            TempData["SuccessMessage"] = response.Message;
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult ResetPassword(string userId, string token)
        {
            return View(new ResetPasswordRequestViewModel { UserId = userId, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.ResetPasswordAsync(model);
            if (!response.Success)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = response.Message;
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult VerifyMfa()
        {
            var challengeToken = TempData["MfaChallengeToken"] as string;
            if (string.IsNullOrEmpty(challengeToken))
            {
                return RedirectToAction(nameof(Login));
            }

            TempData.Keep("MfaChallengeToken");
            return View(new VerifyMfaRequestViewModel { MfaChallengeToken = challengeToken });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyMfa(VerifyMfaRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.VerifyMfaAsync(model);
            if (!response.Success || response.Data == null)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                return View(model);
            }

            await SignInUserAsync(response.Data);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> MfaSetup()
        {
            var accessToken = User.FindFirst("AccessToken")?.Value ?? string.Empty;
            var response = await _apiService.EnableMfaAsync(accessToken);

            if (!response.Success || response.Data == null)
            {
                TempData["ErrorMessage"] = response.Message;
                return RedirectToAction("Index", "Home");
            }

            return View(response.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = User.FindFirst("RefreshToken")?.Value ?? string.Empty;
            var accessToken = User.FindFirst("AccessToken")?.Value ?? string.Empty;

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _apiService.LogoutAsync(new RefreshTokenRequestViewModel { RefreshToken = refreshToken }, accessToken);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        private async Task SignInUserAsync(AuthResponseViewModel auth)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, auth.UserId),
                new Claim(ClaimTypes.Email, auth.Email),
                new Claim(ClaimTypes.Name, auth.FullName),
                new Claim("AccessToken", auth.AccessToken),
                new Claim("RefreshToken", auth.RefreshToken)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}

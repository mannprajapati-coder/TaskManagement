using System;
using Microsoft.AspNetCore.Http;

namespace TaskPlatform.Web.Helpers
{
    /// <summary>
    /// Persists the user's globally-selected workspace across every page (navbar switcher),
    /// since the app is server-rendered MVC with no client-side global store.
    /// </summary>
    public static class WorkspaceCookie
    {
        public const string CookieName = "tp_current_workspace";

        public static Guid? Get(HttpContext context)
        {
            return Guid.TryParse(context.Request.Cookies[CookieName], out var id) ? id : (Guid?)null;
        }

        public static void Set(HttpContext context, Guid workspaceId)
        {
            context.Response.Cookies.Append(CookieName, workspaceId.ToString(), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(180),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });
        }
    }
}

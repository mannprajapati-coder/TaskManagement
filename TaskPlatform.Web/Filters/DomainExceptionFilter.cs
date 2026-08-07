using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;
using TaskPlatform.Shared.Exceptions;

namespace TaskPlatform.Web.Filters
{
    public class DomainExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is DomainException de)
            {
                if (IsAjaxRequest(context.HttpContext.Request))
                {
                    context.Result = new JsonResult(new
                    {
                        success = false,
                        message = de.Message
                    })
                    {
                        StatusCode = (int)HttpStatusCode.OK
                    };
                }
                else
                {
                    var tempDataFactory = context.HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory)) as Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory;
                    var tempData = tempDataFactory?.GetTempData(context.HttpContext);
                    if (tempData != null)
                    {
                        tempData["ErrorMessage"] = de.Message;
                    }

                    var referrer = context.HttpContext.Request.Headers["Referer"].ToString();
                    context.Result = !string.IsNullOrEmpty(referrer)
                        ? new RedirectResult(referrer)
                        : new RedirectToActionResult("Index", "Home", null);
                }

                context.ExceptionHandled = true;
            }
        }

        private static bool IsAjaxRequest(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   (request.ContentType != null && request.ContentType.Contains("application/json", System.StringComparison.OrdinalIgnoreCase)) ||
                   (request.Headers["Accept"].ToString().Contains("application/json", System.StringComparison.OrdinalIgnoreCase));
        }
    }
}

using InertiaCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace PingCRM.Middleware;

public class CsrfMiddleware
{
    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<CsrfMiddleware>>();
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var method = context.Request.Method.ToUpperInvariant();
        var isSafeMethod = method == "GET" || method == "HEAD" || method == "OPTIONS" || method == "TRACE";

        // Validate CSRF for state-changing HTTP methods
        if (!isSafeMethod)
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException ex)
            {
                var isInertiaRequest = context.Request.Headers["X-Inertia"].ToString() == "true";
                var referrer = context.Request.Headers["Referer"].ToString() ?? "/";
                var locationKey = isInertiaRequest ? "X-Inertia-Location" : "Location";

                logger.LogWarning("CSRF Middleware: Token validation failed - {Message}", ex.Message);

                // Put flash message into TempData
                var factory = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
                var tempData = factory.GetTempData(context);
                tempData["error"] = "The page expired, please try again.";
                tempData.Save();

                context.Response.StatusCode = isInertiaRequest ? 409 : 303;
                context.Response.Headers[locationKey] = referrer;
                return;
            }
        }

        var tokens = antiforgery.GetAndStoreTokens(context);

        // Put the *request* token into a cookie that JS can read so Axios can echo it back
        // IMPORTANT: httpOnly=false so the browser will expose it to JS (Axios).
        var cookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        };

        context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, cookieOptions);

        await _next(context);
    }
}

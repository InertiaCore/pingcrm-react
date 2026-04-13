using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PingCRM.Data;
using PingCRM.Models;

namespace PingCRM.Middleware;

public class SessionTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private const string SessionCookieName = "PingCRM.SessionId";

    public SessionTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var sessionToken = context.Request.Cookies[SessionCookieName];

        if (!string.IsNullOrEmpty(sessionToken))
        {
            var session = await dbContext.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

            if (session == null)
            {
                // Session was revoked — sign the user out
                var signInManager = context.RequestServices.GetRequiredService<SignInManager<User>>();
                await signInManager.SignOutAsync();
                context.Response.Cookies.Delete(SessionCookieName);
                context.Response.Redirect("/login");
                return;
            }

            // Throttle updates to once per minute to avoid excessive writes
            if ((DateTime.UtcNow - session.LastActivityAt).TotalMinutes >= 1)
            {
                session.LastActivityAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            // Store session ID in HttpContext for other middleware/controllers
            context.Items["CurrentSessionId"] = session.Id;
        }

        await _next(context);
    }

    /// <summary>
    /// Creates a new session record and sets the session cookie.
    /// Call this from the login controller after successful authentication.
    /// </summary>
    public static async Task CreateSessionAsync(HttpContext context, int userId)
    {
        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var session = new UserSession
        {
            UserId = userId,
            SessionToken = token,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context.Request.Headers.UserAgent.ToString().Length > 512
                ? context.Request.Headers.UserAgent.ToString()[..512]
                : context.Request.Headers.UserAgent.ToString(),
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();

        context.Response.Cookies.Append(SessionCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(14),
            Path = "/"
        });
    }

    /// <summary>
    /// Deletes the current session record and cookie.
    /// Call this from the logout controller.
    /// </summary>
    public static async Task DestroySessionAsync(HttpContext context)
    {
        var sessionToken = context.Request.Cookies[SessionCookieName];
        if (string.IsNullOrEmpty(sessionToken)) return;

        var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var session = await dbContext.UserSessions
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);

        if (session != null)
        {
            dbContext.UserSessions.Remove(session);
            await dbContext.SaveChangesAsync();
        }

        context.Response.Cookies.Delete(SessionCookieName);
    }
}

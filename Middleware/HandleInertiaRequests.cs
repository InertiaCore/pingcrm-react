using InertiaCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using PingCRM.Data;
using PingCRM.Models;

namespace PingCRM.Middleware;

public class HandleInertiaRequests
{
    private readonly RequestDelegate _next;

    public HandleInertiaRequests(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Inertia.Share("flash", () =>
        {
            try
            {
                var factory = context.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
                var tempData = factory.GetTempData(context);
                var flash = new Dictionary<string, object>();

                if (tempData.TryGetValue("error", out var error) && error is not null)
                {
                    // Ensure we only store simple serializable types
                    flash.Add("error", error.ToString() ?? "");
                }
                if (tempData.TryGetValue("success", out var success) && success is not null)
                {
                    // Ensure we only store simple serializable types
                    flash.Add("success", success.ToString() ?? "");
                }
                return flash;
            }
            catch (Exception)
            {
                // If there's any issue with TempData, return empty flash to prevent crashes
                return new Dictionary<string, object>();
            }
        });

        // Resolve user data async before the lambda to avoid sync-over-async (.Result)
        object authData;
        User? currentUser = null;
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<User>>();
            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            currentUser = await userManager.GetUserAsync(context.User);
            if (currentUser != null)
            {
                var account = await dbContext.Accounts.FindAsync(currentUser.AccountId);
                authData = new
                {
                    user = new
                    {
                        id = currentUser.Id,
                        first_name = currentUser.FirstName,
                        last_name = currentUser.LastName,
                        email = currentUser.Email,
                        owner = currentUser.Owner,
                        photo = currentUser.PhotoPath,
                        deleted_at = currentUser.DeletedAt,
                        email_verified = currentUser.EmailConfirmed,
                        two_factor_enabled = currentUser.TwoFactorEnabled,
                        account = new
                        {
                            id = currentUser.AccountId,
                            name = account?.Name
                        }
                    }
                };
            }
            else
            {
                authData = new { user = (object?)null };
            }
        }
        else
        {
            authData = new { user = (object?)null };
        }

        Inertia.Share("auth", () => authData);

        // Redirect unverified users to the verification notice page
        if (currentUser != null && !currentUser.EmailConfirmed)
        {
            var path = context.Request.Path.Value ?? "";
            var isAllowed = path.StartsWith("/verify-email") ||
                            path.StartsWith("/email/verification-notification") ||
                            path == "/logout";

            if (!isAllowed)
            {
                context.Response.Redirect("/verify-email");
                return;
            }
        }

        await _next(context);
    }
}

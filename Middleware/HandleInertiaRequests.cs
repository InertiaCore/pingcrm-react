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
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userManager = context.RequestServices.GetRequiredService<UserManager<User>>();
            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await userManager.GetUserAsync(context.User);
            if (user != null)
            {
                var account = await dbContext.Accounts.FindAsync(user.AccountId);
                authData = new
                {
                    user = new
                    {
                        id = user.Id,
                        first_name = user.FirstName,
                        last_name = user.LastName,
                        email = user.Email,
                        owner = user.Owner,
                        photo = user.PhotoPath,
                        deleted_at = user.DeletedAt,
                        email_verified = user.EmailConfirmed,
                        account = new
                        {
                            id = user.AccountId,
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

        await _next(context);
    }
}

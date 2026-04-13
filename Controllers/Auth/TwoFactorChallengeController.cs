using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using InertiaCore;
using PingCRM.Middleware;
using PingCRM.Models;
using PingCRM.Services;

namespace PingCRM.Controllers.Auth
{
    [EnableRateLimiting("auth")]
    public class TwoFactorChallengeController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly IAuditService _auditService;

        public TwoFactorChallengeController(SignInManager<User> signInManager, IAuditService auditService)
        {
            _signInManager = signInManager;
            _auditService = auditService;
        }

        [HttpGet]
        [Route("two-factor-challenge")]
        public async Task<IActionResult> Create()
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction("Create", "AuthenticatedSession");
            }

            return Inertia.Render("Auth/TwoFactorChallenge");
        }

        [HttpPost]
        [Route("two-factor-challenge")]
        public async Task<IActionResult> Store([FromBody] TwoFactorChallengeRequest request)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction("Create", "AuthenticatedSession");
            }

            Microsoft.AspNetCore.Identity.SignInResult result;

            if (!string.IsNullOrEmpty(request.RecoveryCode))
            {
                result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(request.RecoveryCode);
            }
            else
            {
                result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                    request.Code ?? "", false, false);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError("code", "The code is invalid.");
                return Inertia.Back();
            }

            await SessionTrackingMiddleware.CreateSessionAsync(HttpContext, user.Id);
            await _auditService.LogAsync(HttpContext, user.Id, "Login", "2FA verified");
            return RedirectToAction("Index", "Dashboard");
        }
    }

    public class TwoFactorChallengeRequest
    {
        public string? Code { get; set; }
        public string? RecoveryCode { get; set; }
    }
}

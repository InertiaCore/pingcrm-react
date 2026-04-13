using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
    public class AuthenticatedSessionController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<AuthenticatedSessionController> _logger;

        public AuthenticatedSessionController(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IAuditService auditService,
            ILogger<AuthenticatedSessionController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        [HttpGet]
        [Route("login")]
        public IActionResult Create()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return Inertia.Render("Auth/Login");
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Store([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("email", "These credentials do not match our records.");
                return Inertia.Back();
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                await _auditService.LogAsync(HttpContext, null, "LoginFailed", $"Unknown email: {request.Email}");
                ModelState.AddModelError("email", "These credentials do not match our records.");
                return Inertia.Back();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                request.Remember,
                lockoutOnFailure: true
            );

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(HttpContext, user.Id, "AccountLockedOut");
                _logger.LogWarning("User account locked out: {Email}", request.Email);
                ModelState.AddModelError("email", "Account locked out. Please try again later.");
                return Inertia.Back();
            }

            if (result.RequiresTwoFactor)
            {
                return RedirectToAction("Create", "TwoFactorChallenge");
            }

            if (!result.Succeeded)
            {
                await _auditService.LogAsync(HttpContext, user.Id, "LoginFailed");
                ModelState.AddModelError("email", "These credentials do not match our records.");
                return Inertia.Back();
            }

            await SessionTrackingMiddleware.CreateSessionAsync(HttpContext, user.Id);
            await _auditService.LogAsync(HttpContext, user.Id, "Login");
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpDelete]
        [Route("logout")]
        [Authorize]
        public async Task<IActionResult> Destroy()
        {
            var user = await _userManager.GetUserAsync(User);
            await SessionTrackingMiddleware.DestroySessionAsync(HttpContext);
            await _signInManager.SignOutAsync();
            if (user != null)
            {
                await _auditService.LogAsync(HttpContext, user.Id, "Logout");
            }
            return RedirectToAction("Create", "AuthenticatedSession");
        }

        [HttpPost]
        [Route("login/passkey/options")]
        public async Task<IActionResult> PasskeyRequestOptions()
        {
            // Pass null for discoverable credential flow (no specific user)
            var optionsJson = await _signInManager.MakePasskeyRequestOptionsAsync(null);
            return Content(optionsJson, "application/json");
        }

        [HttpPost]
        [Route("login/passkey")]
        public async Task<IActionResult> PasskeySignIn([FromBody] PasskeySignInRequest request)
        {
            var result = await _signInManager.PasskeySignInAsync(request.CredentialJson);

            if (!result.Succeeded)
            {
                return BadRequest(new { error = "Passkey authentication failed." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await SessionTrackingMiddleware.CreateSessionAsync(HttpContext, user.Id);
                await _auditService.LogAsync(HttpContext, user.Id, "Login", "Passkey");
            }

            return Ok(new { redirect = "/" });
        }
    }

    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public bool Remember { get; set; }
    }

    public class PasskeyOptionsRequest
    {
        public string? Email { get; set; }
    }

    public class PasskeySignInRequest
    {
        public required string CredentialJson { get; set; }
    }
}

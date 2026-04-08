using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InertiaCore;
using PingCRM.Models;

namespace PingCRM.Controllers.Auth
{
    public class AuthenticatedSessionController : Controller
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AuthenticatedSessionController> _logger;

        public AuthenticatedSessionController(SignInManager<User> signInManager, UserManager<User> userManager, ILogger<AuthenticatedSessionController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
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
                _logger.LogWarning("Invalid login attempt for user {Email}", request.Email);
                ModelState.AddModelError("email", "These credentials do not match our records.");
                return Inertia.Back();
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Invalid login attempt for user {Email}", request.Email);
                ModelState.AddModelError("email", "These credentials do not match our records.");
                return Inertia.Back();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                request.Password,
                request.Remember,
                lockoutOnFailure: false
            );

            if (!result.Succeeded)
            {
                _logger.LogWarning("Invalid login attempt for user {Email}", request.Email);
                ModelState.AddModelError("email", "These credentials do not match our records.");
                return Inertia.Back();
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpDelete]
        [Route("logout")]
        [Authorize]
        public async Task<IActionResult> Destroy()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Create", "AuthenticatedSession");
        }
    }

    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public bool Remember { get; set; }
    }
}

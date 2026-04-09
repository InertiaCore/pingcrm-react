using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using InertiaCore;
using PingCRM.Models;
using PingCRM.Services;

namespace PingCRM.Controllers.Auth
{
    [EnableRateLimiting("auth")]
    public class PasswordResetController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetController> _logger;

        public PasswordResetController(UserManager<User> userManager, IEmailService emailService, ILogger<PasswordResetController> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        [Route("forgot-password")]
        public IActionResult Create()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return Inertia.Render("Auth/ForgotPassword");
        }

        [HttpPost]
        [Route("forgot-password")]
        public async Task<IActionResult> Store([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Inertia.Back();
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetUrl = Url.Action("Edit", "PasswordReset", new { token, email = request.Email }, Request.Scheme);

                await _emailService.SendEmailAsync(
                    request.Email,
                    "Reset Your Password - PingCRM",
                    EmailTemplates.PasswordReset(resetUrl!));
            }

            // Always show success to prevent email enumeration
            TempData["success"] = "If an account exists with that email, we've sent a password reset link.";
            return Inertia.Back();
        }

        [HttpGet]
        [Route("reset-password")]
        public IActionResult Edit(string token, string email)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return Inertia.Render("Auth/ResetPassword", new
            {
                Token = token,
                Email = email
            });
        }

        [HttpPost]
        [Route("reset-password")]
        public async Task<IActionResult> Update([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Inertia.Back();
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                ModelState.AddModelError("email", "We can't find a user with that email address.");
                return Inertia.Back();
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("email", error.Description);
                }
                return Inertia.Back();
            }

            TempData["success"] = "Your password has been reset. You can now sign in.";
            return RedirectToAction("Create", "AuthenticatedSession");
        }
    }

    public class ForgotPasswordRequest
    {
        public required string Email { get; set; }
    }

    public class ResetPasswordRequest
    {
        public required string Email { get; set; }
        public required string Token { get; set; }
        public required string Password { get; set; }
        public required string PasswordConfirmation { get; set; }
    }
}

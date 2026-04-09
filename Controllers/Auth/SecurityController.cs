using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InertiaCore;
using PingCRM.Models;
using PingCRM.Services;

namespace PingCRM.Controllers.Auth
{
    [Authorize]
    public class SecurityController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailService _emailService;

        public SecurityController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        [HttpGet]
        [Route("settings/security")]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            return Inertia.Render("Settings/Security", new
            {
                PendingEmail = (string?)TempData.Peek("pending_email")
            });
        }

        [HttpPut]
        [Route("settings/security/password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Inertia.Back();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (request.Password != request.PasswordConfirmation)
            {
                ModelState.AddModelError("password", "The password confirmation does not match.");
                return Inertia.Back();
            }

            var checkResult = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!checkResult)
            {
                ModelState.AddModelError("current_password", "The current password is incorrect.");
                return Inertia.Back();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("password", error.Description);
                }
                return Inertia.Back();
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["success"] = "Password updated.";
            return Inertia.Back();
        }

        [HttpPut]
        [Route("settings/security/email")]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Inertia.Back();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var checkResult = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!checkResult)
            {
                ModelState.AddModelError("email_password", "The password is incorrect.");
                return Inertia.Back();
            }

            if (string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("email", "This is already your email address.");
                return Inertia.Back();
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("email", "This email address is already in use.");
                return Inertia.Back();
            }

            var token = await _userManager.GenerateChangeEmailTokenAsync(user, request.Email);
            var verifyUrl = Url.Action("ConfirmEmailChange", "Security",
                new { userId = user.Id, email = request.Email, token }, Request.Scheme);

            await _emailService.SendEmailAsync(
                request.Email,
                "Confirm Your New Email Address - PingCRM",
                EmailTemplates.EmailChange(verifyUrl!));

            TempData["success"] = "A confirmation link has been sent to your new email address.";
            return Inertia.Back();
        }

        [HttpGet]
        [Route("settings/security/email/confirm")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailChange(int userId, string email, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                TempData["error"] = "Invalid verification link.";
                return RedirectToAction("Create", "AuthenticatedSession");
            }

            var result = await _userManager.ChangeEmailAsync(user, email, token);
            if (!result.Succeeded)
            {
                TempData["error"] = "The verification link is invalid or has expired.";
                return RedirectToAction("Edit");
            }

            // Keep username in sync with email
            user.UserName = email;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // If the current user changed their own email, refresh the session
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Id == user.Id)
                {
                    await _signInManager.RefreshSignInAsync(user);
                }
            }

            TempData["success"] = "Your email address has been updated.";
            return RedirectToAction("Edit");
        }
    }

    public class UpdatePasswordRequest
    {
        public required string CurrentPassword { get; set; }
        public required string Password { get; set; }
        public required string PasswordConfirmation { get; set; }
    }

    public class UpdateEmailRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}

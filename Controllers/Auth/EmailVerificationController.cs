using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using InertiaCore;
using PingCRM.Models;
using PingCRM.Services;

namespace PingCRM.Controllers.Auth
{
    [Authorize]
    public class EmailVerificationController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IEmailService _emailService;

        public EmailVerificationController(UserManager<User> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [HttpGet]
        [Route("verify-email/{userId}/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(int userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                TempData["error"] = "The verification link is invalid or has expired.";
                return RedirectToAction("Index", "Dashboard");
            }

            TempData["success"] = "Your email address has been verified.";
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [Route("email/verification-notification")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Resend()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (user.EmailConfirmed)
            {
                TempData["success"] = "Your email is already verified.";
                return Inertia.Back();
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var verifyUrl = Url.Action("Verify", "EmailVerification",
                new { userId = user.Id, token }, Request.Scheme);

            await _emailService.SendEmailAsync(
                user.Email!,
                "Verify Your Email Address - PingCRM",
                EmailTemplates.EmailVerification(verifyUrl!));

            TempData["success"] = "A new verification link has been sent to your email address.";
            return Inertia.Back();
        }
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using InertiaCore;
using PingCRM.Models;

namespace PingCRM.Controllers.Auth
{
    [Authorize]
    public class SecurityController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public SecurityController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        [Route("settings/security")]
        public IActionResult Edit()
        {
            return Inertia.Render("Settings/Security");
        }

        [HttpPut]
        [Route("settings/security")]
        public async Task<IActionResult> Update([FromBody] UpdatePasswordRequest request)
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

            // Refresh the sign-in cookie so the security stamp change doesn't log them out
            await _signInManager.RefreshSignInAsync(user);

            TempData["success"] = "Password updated.";
            return Inertia.Back();
        }
    }

    public class UpdatePasswordRequest
    {
        public required string CurrentPassword { get; set; }
        public required string Password { get; set; }
        public required string PasswordConfirmation { get; set; }
    }
}

using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InertiaCore;
using PingCRM.Data;
using PingCRM.Models;
using PingCRM.Services;

namespace PingCRM.Controllers.Auth
{
    [Authorize]
    public class SecurityController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public SecurityController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context,
            IEmailService emailService,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailService = emailService;
            _auditService = auditService;
        }

        [HttpGet]
        [Route("settings/security")]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var currentSessionId = HttpContext.Items["CurrentSessionId"] as int?;
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == user.Id)
                .OrderByDescending(s => s.LastActivityAt)
                .Select(s => new
                {
                    s.Id,
                    ip_address = s.IpAddress,
                    user_agent = s.UserAgent,
                    last_activity_at = s.LastActivityAt,
                    is_current = s.Id == currentSessionId
                })
                .ToListAsync();

            var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            var recoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);

            var passkeys = await _userManager.GetPasskeysAsync(user);
            var passkeyList = passkeys.Select(p => new
            {
                credential_id = Convert.ToBase64String(p.CredentialId),
                name = p.Name ?? "Unnamed passkey",
                created_at = p.CreatedAt
            }).ToList();

            return Inertia.Render("Settings/Security", new
            {
                Sessions = sessions,
                TwoFactorEnabled = twoFactorEnabled,
                RecoveryCodesLeft = recoveryCodesLeft,
                Passkeys = passkeyList
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
            await _auditService.LogAsync(HttpContext, user.Id, "PasswordChanged");

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

            await _auditService.LogAsync(HttpContext, user.Id, "EmailChangeRequested", $"New: {request.Email}");

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

            await _auditService.LogAsync(HttpContext, user.Id, "EmailChanged", $"New: {email}");

            TempData["success"] = "Your email address has been updated.";
            return RedirectToAction("Edit");
        }

        // --- Session Management ---

        [HttpDelete]
        [Route("settings/security/sessions/{id}")]
        public async Task<IActionResult> RevokeSession(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var session = await _context.UserSessions.FindAsync(id);
            if (session == null || session.UserId != user.Id)
            {
                return NotFound();
            }

            var currentSessionId = HttpContext.Items["CurrentSessionId"] as int?;
            if (session.Id == currentSessionId)
            {
                TempData["error"] = "You cannot revoke your current session.";
                return Inertia.Back();
            }

            _context.UserSessions.Remove(session);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync(HttpContext, user.Id, "SessionRevoked");

            TempData["success"] = "Session revoked.";
            return Inertia.Back();
        }

        // --- Two-Factor Authentication ---

        [HttpPost]
        [Route("settings/security/two-factor/setup")]
        public async Task<IActionResult> SetupTwoFactor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            await _userManager.ResetAuthenticatorKeyAsync(user);
            var key = await _userManager.GetAuthenticatorKeyAsync(user);

            var uri = $"otpauth://totp/PingCRM:{Uri.EscapeDataString(user.Email!)}?secret={key}&issuer=PingCRM&digits=6";

            return Inertia.Render("Settings/Security", new
            {
                Sessions = Array.Empty<object>(),
                TwoFactorEnabled = false,
                RecoveryCodesLeft = 0,
                TwoFactorSetup = new { key, uri }
            });
        }

        [HttpPost]
        [Route("settings/security/two-factor/enable")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorVerifyRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.Code);

            if (!isValid)
            {
                ModelState.AddModelError("code", "The verification code is invalid.");
                return Inertia.Back();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            await _signInManager.RefreshSignInAsync(user);
            await _auditService.LogAsync(HttpContext, user.Id, "TwoFactorEnabled");

            TempData["success"] = "Two-factor authentication has been enabled.";

            return Inertia.Render("Settings/Security", new
            {
                Sessions = Array.Empty<object>(),
                TwoFactorEnabled = true,
                RecoveryCodesLeft = 10,
                RecoveryCodes = recoveryCodes!.ToArray()
            });
        }

        [HttpPost]
        [Route("settings/security/two-factor/disable")]
        public async Task<IActionResult> DisableTwoFactor([FromBody] TwoFactorDisableRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var checkResult = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!checkResult)
            {
                ModelState.AddModelError("password", "The password is incorrect.");
                return Inertia.Back();
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            await _auditService.LogAsync(HttpContext, user.Id, "TwoFactorDisabled");

            TempData["success"] = "Two-factor authentication has been disabled.";
            return RedirectToAction("Edit");
        }

        [HttpPost]
        [Route("settings/security/two-factor/recovery-codes")]
        public async Task<IActionResult> RegenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!await _userManager.GetTwoFactorEnabledAsync(user))
            {
                TempData["error"] = "Two-factor authentication is not enabled.";
                return Inertia.Back();
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

            return Inertia.Render("Settings/Security", new
            {
                Sessions = Array.Empty<object>(),
                TwoFactorEnabled = true,
                RecoveryCodesLeft = 10,
                RecoveryCodes = recoveryCodes!.ToArray()
            });
        }

        // --- Passkeys ---

        [HttpPost]
        [Route("settings/security/passkeys/options")]
        public async Task<IActionResult> PasskeyCreationOptions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var userId = await _userManager.GetUserIdAsync(user);
            var userName = await _userManager.GetUserNameAsync(user) ?? "User";

            var optionsJson = await _signInManager.MakePasskeyCreationOptionsAsync(new()
            {
                Id = userId,
                Name = userName,
                DisplayName = $"{user.FirstName} {user.LastName}"
            });

            return Content(optionsJson, "application/json");
        }

        [HttpPost]
        [Route("settings/security/passkeys")]
        public async Task<IActionResult> StorePasskey([FromBody] PasskeyCredentialRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var attestationResult = await _signInManager.PerformPasskeyAttestationAsync(request.CredentialJson);
            if (!attestationResult.Succeeded)
            {
                return BadRequest(new { error = attestationResult.Failure?.Message ?? "Passkey verification failed." });
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                attestationResult.Passkey.Name = request.Name;
            }

            var result = await _userManager.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey);
            if (!result.Succeeded)
            {
                return BadRequest(new { error = "Failed to store passkey." });
            }

            await _auditService.LogAsync(HttpContext, user.Id, "PasskeyAdded", attestationResult.Passkey.Name);

            TempData["success"] = "Passkey added.";
            return Ok();
        }

        [HttpDelete]
        [Route("settings/security/passkeys/{credentialId}")]
        public async Task<IActionResult> DestroyPasskey(string credentialId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var passkeys = await _userManager.GetPasskeysAsync(user);
            var passkey = passkeys.FirstOrDefault(p =>
                Convert.ToBase64String(p.CredentialId) == credentialId);

            if (passkey == null) return NotFound();

            await _userManager.RemovePasskeyAsync(user, passkey.CredentialId);
            await _auditService.LogAsync(HttpContext, user.Id, "PasskeyRemoved", passkey.Name);

            TempData["success"] = "Passkey removed.";
            return Inertia.Back();
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

    public class TwoFactorVerifyRequest
    {
        public required string Code { get; set; }
    }

    public class TwoFactorDisableRequest
    {
        public required string Password { get; set; }
    }

    public class PasskeyCredentialRequest
    {
        public required string CredentialJson { get; set; }
        public string? Name { get; set; }
    }
}

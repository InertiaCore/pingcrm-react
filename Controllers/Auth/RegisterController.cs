using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using InertiaCore;
using PingCRM.Data;
using PingCRM.Models;
using PingCRM.Services;

namespace PingCRM.Controllers.Auth
{
    [EnableRateLimiting("auth")]
    public class RegisterController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;

        public RegisterController(
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
        [Route("register")]
        public IActionResult Create()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return Inertia.Render("Auth/Register");
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Store([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Inertia.Back();
            }

            if (request.Password != request.PasswordConfirmation)
            {
                ModelState.AddModelError("password_confirmation", "The password confirmation does not match.");
                return Inertia.Back();
            }

            // Create a new account for this user
            var account = new Account
            {
                Name = $"{request.FirstName}'s Company",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var user = new User
            {
                AccountId = account.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                UserName = request.Email,
                Owner = true,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                // Clean up the account if user creation fails
                _context.Accounts.Remove(account);
                await _context.SaveChangesAsync();

                foreach (var error in result.Errors)
                {
                    var field = error.Code.Contains("Password") ? "password" : "email";
                    ModelState.AddModelError(field, error.Description);
                }
                return Inertia.Back();
            }

            // Send verification email
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var verifyUrl = Url.Action("Verify", "EmailVerification",
                new { userId = user.Id, token }, Request.Scheme);

            await _emailService.SendEmailAsync(
                user.Email!,
                "Verify Your Email Address - PingCRM",
                EmailTemplates.EmailVerification(verifyUrl!));

            await _signInManager.SignInAsync(user, isPersistent: false);
            await _auditService.LogAsync(HttpContext, user.Id, "AccountCreated");

            return RedirectToAction("Index", "Dashboard");
        }
    }

    public class RegisterRequest
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string PasswordConfirmation { get; set; }
    }
}

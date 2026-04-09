using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InertiaCore;
using PingCRM.Data;
using PingCRM.Models;
using PingCRM.ViewModels;
using PingCRM.ViewModels.Shared;
using PingCRM.Helpers;
using PingCRM.Extensions;
using PingCRM.Services;

namespace PingCRM.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;

        public UsersController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IWebHostEnvironment environment,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
            _emailService = emailService;
        }

        [HttpGet]
        [Route("users")]
        public async Task<IActionResult> Index(string? search, string? role, string? trashed, int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null)
            {
                return Unauthorized();
            }

            const int pageSize = 10;

            var query = _context.Users
                .Where(u => u.AccountId == currentUser.AccountId);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    (u.FirstName ?? "").Contains(search) ||
                    (u.LastName ?? "").Contains(search) ||
                    (u.Email ?? "").Contains(search));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = role switch
                {
                    "user" => query.Where(u => !u.Owner),
                    "owner" => query.Where(u => u.Owner),
                    _ => query
                };
            }

            if (!string.IsNullOrEmpty(trashed))
            {
                if (trashed == "only")
                {
                    query = query.Where(u => u.DeletedAt != null);
                }
            }
            else
            {
                query = query.Where(u => u.DeletedAt == null);
            }

            var total = await query.CountAsync();
            var users = await query
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email ?? string.Empty,
                    Owner = u.Owner,
                    Photo = u.PhotoPath != null ? $"/img/{u.PhotoPath}?w=40&h=40&fit=crop" : null,
                    DeletedAt = u.DeletedAt
                })
                .ToListAsync();

            var paginatedList = new PaginatedList<UserListDto>(users, total, page, pageSize);

            return Inertia.Render("Users/Index", new
            {
                Filters = new UserFilters { Search = search, Role = role, Trashed = trashed },
                Users = new Func<object>(() => paginatedList.ToPaginatedData()),
            });
        }

        [HttpGet]
        [Route("users/create")]
        public IActionResult Create()
        {
            return Inertia.Render("Users/Create");
        }

        [HttpPost]
        [Route("users")]
        public async Task<IActionResult> Store([FromBody] UserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Inertia.Back();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null)
            {
                return Unauthorized();
            }

            if (!currentUser.Owner)
            {
                TempData["error"] = "Only account owners can create users.";
                return Inertia.Back();
            }

            var user = new User
            {
                AccountId = currentUser.AccountId.Value,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                Owner = model.Owner,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (model.Photo != null)
            {
                user.PhotoPath = await SavePhotoAsync(model.Photo);
            }

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
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

            TempData["success"] = "User created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("users/{id}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(id);

            if (user == null || user.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            return Inertia.Render("Users/Edit", new
            {
                User = new
                {
                    user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    user.Email,
                    user.Owner,
                    Photo = user.PhotoPath != null ? $"/img/{user.PhotoPath}?w=60&h=60&fit=crop" : null,
                    DeletedAt = user.DeletedAt
                }
            });
        }

        [HttpPut]
        [Route("users/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(id);

            if (user == null || user.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            if (_environment.EnvironmentName == "Demo" && user.IsDemoUser())
            {
                TempData["error"] = "Updating the demo user is not allowed.";
                return Inertia.Back();
            }

            if (!ModelState.IsValid)
            {
                return Inertia.Back();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.UpdatedAt = DateTime.UtcNow;

            // Only owners can change the Owner flag
            var ownerChanged = model.Owner != user.Owner;
            if (ownerChanged)
            {
                if (!currentUser.Owner)
                {
                    TempData["error"] = "Only account owners can change user roles.";
                    return Inertia.Back();
                }
                user.Owner = model.Owner;
            }

            if (model.Photo != null)
            {
                user.PhotoPath = await SavePhotoAsync(model.Photo);
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, model.Password);
            }

            await _context.SaveChangesAsync();

            // Refresh the session if the current user's own role changed
            if (ownerChanged && user.Id == currentUser.Id)
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            TempData["success"] = "User updated.";
            return Inertia.Back();
        }

        [HttpDelete]
        [Route("users/{id}")]
        public async Task<IActionResult> Destroy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(id);

            if (user == null || user.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            if (user.Id == currentUser.Id)
            {
                TempData["error"] = "You cannot delete yourself.";
                return Inertia.Back();
            }

            if (_environment.EnvironmentName == "Demo" && user.IsDemoUser())
            {
                TempData["error"] = "Deleting the demo user is not allowed.";
                return Inertia.Back();
            }

            user.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["success"] = "User deleted.";
            return Inertia.Back();
        }

        [HttpPut]
        [Route("users/{id}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(id);

            if (user == null || user.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            user.DeletedAt = null;
            await _context.SaveChangesAsync();

            TempData["success"] = "User restored.";
            return Inertia.Back();
        }

        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
        private const long MaxPhotoSize = 5 * 1024 * 1024; // 5 MB

        private async Task<string> SavePhotoAsync(IFormFile photo)
        {
            if (photo.Length > MaxPhotoSize)
            {
                throw new InvalidOperationException("Photo must be less than 5 MB.");
            }

            var extension = System.IO.Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Photo must be a JPG, PNG, or WebP image.");
            }

            if (!AllowedContentTypes.Contains(photo.ContentType))
            {
                throw new InvalidOperationException("Photo must be a JPG, PNG, or WebP image.");
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = System.IO.Path.Combine(_environment.WebRootPath, "uploads", "users", fileName);

            var directoryPath = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }

            using (var stream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            return $"users/{fileName}";
        }
    }
}

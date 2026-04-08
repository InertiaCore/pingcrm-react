using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InertiaCore;
using PingCRM.Data;
using PingCRM.Models;
using PingCRM.ViewModels;
using PingCRM.Helpers;
using PingCRM.ViewModels.Shared;
using PingCRM.Extensions;
using System.Collections.Generic;

namespace PingCRM.Controllers
{
    [Authorize]
    public class OrganizationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public OrganizationsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("organizations")]
        public async Task<IActionResult> Index(string? search, string? trashed, int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null)
            {
                return Unauthorized();
            }
            const int pageSize = 10;

            var query = _context.Organizations
                .Where(o => o.AccountId == currentUser.AccountId);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.Name.Contains(search));
            }

            if (!string.IsNullOrEmpty(trashed))
            {
                if (trashed == "only")
                {
                    query = query.Where(o => o.DeletedAt != null);
                }
            }
            else
            {
                query = query.Where(o => o.DeletedAt == null);
            }

            var total = await query.CountAsync();
            var organizations = await query
                .OrderBy(o => o.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrganizationDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    Phone = o.Phone,
                    City = o.City,
                    DeletedAt = o.DeletedAt
                })
                .ToListAsync();

            var paginatedList = new PaginatedList<OrganizationDto>(organizations, total, page, pageSize);

            return Inertia.Render("Organizations/Index", new
            {
                Filters = new SearchFilters { Search = search, Trashed = trashed },
                Organizations = paginatedList.ToPaginatedData()
            });
        }

        [HttpGet]
        [Route("organizations/create")]
        public IActionResult Create()
        {
            return Inertia.Render("Organizations/Create");
        }

        [HttpPost]
        [Route("organizations")]
        public async Task<IActionResult> Store([FromBody] OrganizationViewModel model)
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

            var organization = new Organization
            {
                AccountId = currentUser.AccountId.Value,
                Name = model.Name,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                Region = model.Region,
                Country = model.Country,
                PostalCode = model.PostalCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            TempData["success"] = "Organization created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("organizations/{id}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var organization = await _context.Organizations
                .Include(o => o.Contacts)
                .FirstOrDefaultAsync(o => o.Id == id && o.AccountId == currentUser.AccountId);

            if (organization == null)
            {
                return NotFound();
            }

            return Inertia.Render("Organizations/Edit", new
            {
                Organization = new OrganizationDetailDto
                {
                    Id = organization.Id,
                    Name = organization.Name,
                    Email = organization.Email,
                    Phone = organization.Phone,
                    Address = organization.Address,
                    City = organization.City,
                    Region = organization.Region,
                    Country = organization.Country,
                    PostalCode = organization.PostalCode,
                    DeletedAt = organization.DeletedAt,
                    Contacts = organization.Contacts?
                        .OrderBy(c => c.LastName)
                        .ThenBy(c => c.FirstName)
                        .Select(c => new ContactSummaryDto
                        {
                            Id = c.Id,
                            Name = c.Name,
                            City = c.City,
                            Phone = c.Phone
                        })
                        .ToList()
                }
            });
        }

        [HttpPut]
        [Route("organizations/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] OrganizationViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var organization = await _context.Organizations.FindAsync(id);

            if (organization == null || organization.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return Inertia.Back();
            }

            organization.Name = model.Name;
            organization.Email = model.Email;
            organization.Phone = model.Phone;
            organization.Address = model.Address;
            organization.City = model.City;
            organization.Region = model.Region;
            organization.Country = model.Country;
            organization.PostalCode = model.PostalCode;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["success"] = "Organization updated.";
            return Inertia.Back();
        }

        [HttpDelete]
        [Route("organizations/{id}")]
        public async Task<IActionResult> Destroy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var organization = await _context.Organizations.FindAsync(id);

            if (organization == null || organization.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            organization.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["success"] = "Organization deleted.";
            return Inertia.Back();
        }

        [HttpPut]
        [Route("organizations/{id}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var organization = await _context.Organizations.FindAsync(id);

            if (organization == null || organization.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            organization.DeletedAt = null;
            await _context.SaveChangesAsync();

            TempData["success"] = "Organization restored.";
            return Inertia.Back();
        }
    }
}

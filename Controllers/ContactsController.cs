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

namespace PingCRM.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ContactsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("contacts")]
        public async Task<IActionResult> Index(string? search, string? trashed, int page = 1)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null)
            {
                return Unauthorized();
            }
            const int pageSize = 10;

            var query = _context.Contacts
                .Include(c => c.Organization)
                .Where(c => c.AccountId == currentUser.AccountId);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c =>
                    (c.FirstName ?? "").Contains(search) ||
                    (c.LastName ?? "").Contains(search) ||
                    (c.Email ?? "").Contains(search) ||
                    (c.Organization != null && (c.Organization.Name ?? "").Contains(search)));
            }

            if (!string.IsNullOrEmpty(trashed))
            {
                if (trashed == "only")
                {
                    query = query.Where(c => c.DeletedAt != null);
                }
            }
            else
            {
                query = query.Where(c => c.DeletedAt == null);
            }

            var total = await query.CountAsync();
            var contacts = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ContactDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    City = c.City,
                    DeletedAt = c.DeletedAt,
                    Organization = c.Organization != null ? new OrganizationSummaryDto { Name = c.Organization.Name } : null
                })
                .ToListAsync();

            var paginatedList = new PaginatedList<ContactDto>(contacts, total, page, pageSize);

            return Inertia.Render("Contacts/Index", new
            {
                Filters = new SearchFilters { Search = search, Trashed = trashed },
                Contacts = paginatedList.ToPaginatedData()
            });
        }

        [HttpGet]
        [Route("contacts/create")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null)
            {
                return Unauthorized();
            }

            var organizations = await _context.Organizations
                .Where(o => o.AccountId == currentUser.AccountId && o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .Select(o => new { o.Id, o.Name })
                .ToListAsync();

            return Inertia.Render("Contacts/Create", new
            {
                Organizations = organizations
            });
        }

        [HttpPost]
        [Route("contacts")]
        public async Task<IActionResult> Store([FromBody] ContactViewModel model)
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

            var contact = new Contact
            {
                AccountId = currentUser.AccountId.Value,
                OrganizationId = model.OrganizationId,
                FirstName = model.FirstName,
                LastName = model.LastName,
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

            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();

            TempData["success"] = "Contact created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("contacts/{id}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var contact = await _context.Contacts.FindAsync(id);

            if (contact == null || contact.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            var organizations = await _context.Organizations
                .Where(o => o.AccountId == currentUser.AccountId && o.DeletedAt == null)
                .OrderBy(o => o.Name)
                .Select(o => new { o.Id, o.Name })
                .ToListAsync();

            return Inertia.Render("Contacts/Edit", new
            {
                Contact = new ContactDetailDto
                {
                    Id = contact.Id,
                    FirstName = contact.FirstName,
                    LastName = contact.LastName,
                    OrganizationId = contact.OrganizationId,
                    Email = contact.Email,
                    Phone = contact.Phone,
                    Address = contact.Address,
                    City = contact.City,
                    Region = contact.Region,
                    Country = contact.Country,
                    PostalCode = contact.PostalCode,
                    DeletedAt = contact.DeletedAt
                },
                Organizations = organizations
            });
        }

        [HttpPut]
        [Route("contacts/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ContactViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var contact = await _context.Contacts.FindAsync(id);

            if (contact == null || contact.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return Inertia.Back();
            }

            contact.OrganizationId = model.OrganizationId;
            contact.FirstName = model.FirstName;
            contact.LastName = model.LastName;
            contact.Email = model.Email;
            contact.Phone = model.Phone;
            contact.Address = model.Address;
            contact.City = model.City;
            contact.Region = model.Region;
            contact.Country = model.Country;
            contact.PostalCode = model.PostalCode;
            contact.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["success"] = "Contact updated.";
            return Inertia.Back();
        }

        [HttpDelete]
        [Route("contacts/{id}")]
        public async Task<IActionResult> Destroy(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var contact = await _context.Contacts.FindAsync(id);

            if (contact == null || contact.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            contact.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["success"] = "Contact deleted.";
            return Inertia.Back();
        }

        [HttpPut]
        [Route("contacts/{id}/restore")]
        public async Task<IActionResult> Restore(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.AccountId == null) return Unauthorized();

            var contact = await _context.Contacts.FindAsync(id);

            if (contact == null || contact.AccountId != currentUser.AccountId)
            {
                return NotFound();
            }

            contact.DeletedAt = null;
            await _context.SaveChangesAsync();

            TempData["success"] = "Contact restored.";
            return Inertia.Back();
        }
    }
}

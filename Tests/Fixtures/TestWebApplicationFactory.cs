using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PingCRM.Data;
using PingCRM.Models;
using PingCRM.Services;

namespace PingCRM.Tests.Fixtures;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Development so ExceptionMiddleware re-throws instead of trying
        // to render Inertia error pages (which need Razor views)
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core and database provider registrations to avoid conflicts
            // between Sqlite (registered by the app) and InMemory (for tests)
            var typesToRemove = new HashSet<Type>
            {
                typeof(DbContextOptions<ApplicationDbContext>),
                typeof(DbContextOptions),
                typeof(IDatabaseInitializationService),
            };

            var descriptorsToRemove = services
                .Where(d => typesToRemove.Contains(d.ServiceType) ||
                            d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in descriptorsToRemove) services.Remove(d);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Re-register Identity stores for the new DbContext
            services.AddScoped<IDatabaseInitializationService, NoOpDatabaseInitializationService>();

            // Replace email service with a fake
            var emailDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null) services.Remove(emailDescriptor);
            services.AddSingleton<IEmailService, FakeEmailService>();
        });
    }

    /// <summary>
    /// Seeds a test account and user, returns the user's credentials.
    /// </summary>
    public async Task<TestUser> SeedTestUserAsync(bool owner = true, bool emailConfirmed = true)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        await context.Database.EnsureCreatedAsync();

        var account = new Account
        {
            Name = "Test Account",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var email = $"test-{Guid.NewGuid():N}@example.com";
        var password = "TestPass1234";

        var user = new User
        {
            AccountId = account.Id,
            FirstName = "Test",
            LastName = "User",
            Email = email,
            UserName = email,
            Owner = owner,
            EmailConfirmed = emailConfirmed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return new TestUser(user.Id, account.Id, email, password);
    }

    /// <summary>
    /// Seeds a second user in the same account.
    /// </summary>
    public async Task<TestUser> SeedUserInAccountAsync(int accountId, bool owner = false, bool emailConfirmed = true)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var email = $"user-{Guid.NewGuid():N}@example.com";
        var password = "TestPass1234";

        var user = new User
        {
            AccountId = accountId,
            FirstName = "Other",
            LastName = "User",
            Email = email,
            UserName = email,
            Owner = owner,
            EmailConfirmed = emailConfirmed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return new TestUser(user.Id, accountId, email, password);
    }

    /// <summary>
    /// Seeds a user in a different account (for tenant isolation tests).
    /// </summary>
    public async Task<TestUser> SeedUserInOtherAccountAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var account = new Account
        {
            Name = "Other Account",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var email = $"other-{Guid.NewGuid():N}@example.com";
        var password = "TestPass1234";

        var user = new User
        {
            AccountId = account.Id,
            FirstName = "Other",
            LastName = "Account",
            Email = email,
            UserName = email,
            Owner = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return new TestUser(user.Id, account.Id, email, password);
    }
}

public record TestUser(int Id, int AccountId, string Email, string Password);

public class FakeEmailService : IEmailService
{
    public List<SentEmail> SentEmails { get; } = [];

    public Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        SentEmails.Add(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

public record SentEmail(string To, string Subject, string HtmlBody);

public class NoOpDatabaseInitializationService : IDatabaseInitializationService
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task<bool> CanConnectAsync() => Task.FromResult(true);
}

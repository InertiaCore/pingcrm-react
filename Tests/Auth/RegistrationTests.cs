using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PingCRM.Services;
using PingCRM.Tests.Fixtures;

namespace PingCRM.Tests.Auth;

public class RegistrationTests : IAsyncLifetime
{
    private TestWebApplicationFactory _factory = null!;
    private readonly Uri _baseUri = new("http://localhost");

    public Task InitializeAsync()
    {
        _factory = new TestWebApplicationFactory();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Register_WithValidData_SignsUserIn()
    {
        var (client, cookies) = _factory.CreateClientWithCookies();

        await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/register", _baseUri, new
        {
            firstName = "Jane",
            lastName = "Doe",
            email = $"jane-{Guid.NewGuid():N}@example.com",
            password = "SecurePass1",
            passwordConfirmation = "SecurePass1"
        });

        // Should be signed in (auth cookie set)
        var authCookie = cookies.GetCookies(_baseUri)["PingCRM.Auth"];
        Assert.NotNull(authCookie);
    }

    [Fact]
    public async Task Register_SendsVerificationEmail()
    {
        var (client, cookies) = _factory.CreateClientWithCookies();
        var email = $"verify-{Guid.NewGuid():N}@example.com";

        await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/register", _baseUri, new
        {
            firstName = "Jane",
            lastName = "Doe",
            email,
            password = "SecurePass1",
            passwordConfirmation = "SecurePass1"
        });

        using var scope = _factory.Services.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>() as FakeEmailService;
        Assert.NotNull(emailService);
        Assert.Contains(emailService.SentEmails, e => e.To == email && e.Subject.Contains("Verify"));
    }

    [Fact]
    public async Task Register_MismatchedPasswords_DoesNotSignIn()
    {
        var (client, cookies) = _factory.CreateClientWithCookies();

        await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/register", _baseUri, new
        {
            firstName = "Jane",
            lastName = "Doe",
            email = $"jane-{Guid.NewGuid():N}@example.com",
            password = "SecurePass1",
            passwordConfirmation = "DifferentPass1"
        });

        var authCookie = cookies.GetCookies(_baseUri)["PingCRM.Auth"];
        Assert.Null(authCookie);
    }

    [Fact]
    public async Task Register_WeakPassword_DoesNotSignIn()
    {
        var (client, cookies) = _factory.CreateClientWithCookies();

        await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/register", _baseUri, new
        {
            firstName = "Jane",
            lastName = "Doe",
            email = $"jane-{Guid.NewGuid():N}@example.com",
            password = "weak",
            passwordConfirmation = "weak"
        });

        var authCookie = cookies.GetCookies(_baseUri)["PingCRM.Auth"];
        Assert.Null(authCookie);
    }
}

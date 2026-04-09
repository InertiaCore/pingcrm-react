using System.Net;
using Microsoft.Extensions.DependencyInjection;
using PingCRM.Services;
using PingCRM.Tests.Fixtures;

namespace PingCRM.Tests.Auth;

public class PasswordResetTests : IAsyncLifetime
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
    public async Task ForgotPassword_ValidEmail_SendsResetEmail()
    {
        var user = await _factory.SeedTestUserAsync();
        var (client, cookies) = _factory.CreateClientWithCookies();

        await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/forgot-password", _baseUri,
            new { email = user.Email });

        using var scope = _factory.Services.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>() as FakeEmailService;
        Assert.NotNull(emailService);
        Assert.Contains(emailService.SentEmails, e => e.To == user.Email && e.Subject.Contains("Reset"));
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_DoesNotSendEmail()
    {
        var (client, cookies) = _factory.CreateClientWithCookies();

        await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/forgot-password", _baseUri,
            new { email = "nonexistent@example.com" });

        using var scope = _factory.Services.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>() as FakeEmailService;
        Assert.NotNull(emailService);
        Assert.DoesNotContain(emailService.SentEmails, e => e.To == "nonexistent@example.com");
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_StillReturnsRedirect()
    {
        // Should not reveal whether email exists (no enumeration)
        var (client, cookies) = _factory.CreateClientWithCookies();

        var response = await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/forgot-password",
            _baseUri, new { email = "nonexistent@example.com" });

        // Should redirect back (success-like response regardless)
        Assert.True(response.StatusCode == HttpStatusCode.Found ||
                    response.StatusCode == HttpStatusCode.SeeOther);
    }
}

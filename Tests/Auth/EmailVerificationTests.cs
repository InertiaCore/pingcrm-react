using System.Net;
using PingCRM.Tests.Fixtures;

namespace PingCRM.Tests.Auth;

public class EmailVerificationTests : IAsyncLifetime
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
    public async Task UnverifiedUser_RedirectedToVerifyEmail()
    {
        var user = await _factory.SeedTestUserAsync(emailConfirmed: false);
        var (client, cookies) = _factory.CreateClientWithCookies();
        await client.AuthenticateAsync(cookies, user.Email, user.Password, _baseUri);

        var response = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/verify-email", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task UnverifiedUser_CanAccessLogout()
    {
        var user = await _factory.SeedTestUserAsync(emailConfirmed: false);
        var (client, cookies) = _factory.CreateClientWithCookies();
        await client.AuthenticateAsync(cookies, user.Email, user.Password, _baseUri);

        var response = await client.SendWithCsrfAsync(cookies, HttpMethod.Delete, "/logout", _baseUri);

        // Should be able to logout (not blocked by email verification)
        Assert.True((int)response.StatusCode is >= 300 and < 400,
            $"Expected redirect after logout, got {response.StatusCode}");
    }
}

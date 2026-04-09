using System.Net;
using PingCRM.Tests.Fixtures;

namespace PingCRM.Tests.Auth;

public class LoginTests : IAsyncLifetime
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
    public async Task Login_WithValidCredentials_Succeeds()
    {
        var user = await _factory.SeedTestUserAsync();
        var (client, cookies) = _factory.CreateClientWithCookies();

        var response = await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/login", _baseUri,
            new { email = user.Email, password = user.Password, remember = false });

        // Should redirect (302/303)
        Assert.True((int)response.StatusCode is >= 300 and < 400,
            $"Expected redirect, got {response.StatusCode}");

        // Auth cookie should be set
        var authCookie = cookies.GetCookies(_baseUri)["PingCRM.Auth"];
        Assert.NotNull(authCookie);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Fails()
    {
        var user = await _factory.SeedTestUserAsync();
        var (client, cookies) = _factory.CreateClientWithCookies();

        await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/login", _baseUri,
            new { email = user.Email, password = "WrongPassword1", remember = false });

        // No auth cookie should be set
        var authCookie = cookies.GetCookies(_baseUri)["PingCRM.Auth"];
        Assert.Null(authCookie);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_Fails()
    {
        var (client, cookies) = _factory.CreateClientWithCookies();

        await client.SendWithCsrfAsync(cookies, HttpMethod.Post, "/login", _baseUri,
            new { email = "nobody@example.com", password = "TestPass1234", remember = false });

        var authCookie = cookies.GetCookies(_baseUri)["PingCRM.Auth"];
        Assert.Null(authCookie);
    }

    [Fact]
    public async Task ProtectedRoute_Unauthenticated_RedirectsToLogin()
    {
        var (client, _) = _factory.CreateClientWithCookies();

        var response = await client.GetAsync("/dashboard");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task ProtectedRoute_InertiaRequest_Unauthenticated_Returns409()
    {
        var (client, _) = _factory.CreateClientWithCookies();

        var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
        request.Headers.Add("X-Inertia", "true");

        var response = await client.SendAsync(request);

        Assert.Equal((HttpStatusCode)409, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Inertia-Location"));
    }

    [Fact]
    public async Task Logout_Authenticated_RedirectsToLogin()
    {
        var user = await _factory.SeedTestUserAsync();
        var (client, cookies) = _factory.CreateClientWithCookies();
        await client.AuthenticateAsync(cookies, user.Email, user.Password, _baseUri);

        var response = await client.SendWithCsrfAsync(cookies, HttpMethod.Delete, "/logout", _baseUri);

        // Should redirect (to login page)
        Assert.True((int)response.StatusCode is >= 300 and < 400,
            $"Expected redirect, got {response.StatusCode}");
    }
}

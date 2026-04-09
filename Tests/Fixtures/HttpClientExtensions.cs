using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PingCRM.Tests.Fixtures;

public static class HttpClientExtensions
{
    /// <summary>
    /// Creates a client with a CookieContainer so we can manually extract cookies.
    /// </summary>
    public static (HttpClient Client, CookieContainer Cookies) CreateClientWithCookies(
        this WebApplicationFactory<Program> factory)
    {
        var cookies = new CookieContainer();
        var client = factory.CreateDefaultClient(
            new CookieContainerHandler(cookies));
        return (client, cookies);
    }

    /// <summary>
    /// Authenticates the client by posting to /login.
    /// </summary>
    public static async Task AuthenticateAsync(
        this HttpClient client, CookieContainer cookies, string email, string password, Uri baseUri)
    {
        await EnsureCsrfTokenAsync(client, cookies, baseUri);
        var csrfToken = GetCsrfToken(cookies, baseUri);

        var request = new HttpRequestMessage(HttpMethod.Post, "/login")
        {
            Content = JsonContent.Create(new { email, password, remember = false })
        };
        if (csrfToken != null) request.Headers.Add("X-XSRF-TOKEN", csrfToken);

        var response = await client.SendAsync(request);

        if ((int)response.StatusCode >= 400)
        {
            throw new Exception($"Login failed with status {response.StatusCode}");
        }
    }

    /// <summary>
    /// Sends a POST/PUT/DELETE with CSRF token.
    /// </summary>
    public static async Task<HttpResponseMessage> SendWithCsrfAsync(
        this HttpClient client, CookieContainer cookies, HttpMethod method, string url,
        Uri baseUri, object? body = null)
    {
        await EnsureCsrfTokenAsync(client, cookies, baseUri);
        var csrfToken = GetCsrfToken(cookies, baseUri);

        var request = new HttpRequestMessage(method, url);
        if (body != null)
        {
            request.Content = JsonContent.Create(body);
        }
        if (csrfToken != null) request.Headers.Add("X-XSRF-TOKEN", csrfToken);

        return await client.SendAsync(request);
    }

    private static async Task EnsureCsrfTokenAsync(HttpClient client, CookieContainer cookies, Uri baseUri)
    {
        if (GetCsrfToken(cookies, baseUri) != null) return;

        // Hit /health (non-Inertia endpoint) to trigger CsrfMiddleware
        // which sets the XSRF-TOKEN and antiforgery cookies.
        await client.GetAsync("/health");
    }

    private static string? GetCsrfToken(CookieContainer cookies, Uri baseUri)
    {
        var allCookies = cookies.GetCookies(baseUri);
        return allCookies["XSRF-TOKEN"]?.Value;
    }
}

internal class CookieContainerHandler : DelegatingHandler
{
    private readonly CookieContainer _cookies;

    public CookieContainerHandler(CookieContainer cookies)
    {
        _cookies = cookies;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Add cookies to request
        var cookieHeader = _cookies.GetCookieHeader(request.RequestUri!);
        if (!string.IsNullOrEmpty(cookieHeader))
        {
            request.Headers.Add("Cookie", cookieHeader);
        }

        HttpResponseMessage response;
        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (Exception)
        {
            // Inertia Render throws in tests (no Razor views). Create a minimal
            // response so test flow can continue.
            response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return response;
        }

        // Extract cookies from response
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var cookie in setCookies)
            {
                try { _cookies.SetCookies(request.RequestUri!, cookie); } catch { }
            }
        }

        return response;
    }
}

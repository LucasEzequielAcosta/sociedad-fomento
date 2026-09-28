using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SociedadFomento.Api.Authentication;
using SociedadFomento.Api.Contracts.Authentication;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies administrator cookie authentication, authorization and antiforgery behavior.</summary>
public sealed class AuthApiTests
{
    /// <summary>Verifies secure login cookie, current identity and logout invalidation.</summary>
    [Fact]
    public async Task LoginMeAndLogout_ManageSecureSession()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        try
        {
            HttpResponseMessage login = await factory.AuthenticateAsync();
            string cookie = login.Headers.GetValues("Set-Cookie")
                .Single(value => value.StartsWith($"{AuthenticationConstants.CookieName}=", StringComparison.Ordinal));
            Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("expires=", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("max-age=", cookie, StringComparison.OrdinalIgnoreCase);

            HttpResponseMessage me = await factory.Client.GetAsync("/api/auth/me");
            JsonDocument identity = await JsonDocument.ParseAsync(await me.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            Assert.Equal(factory.AdminEmail, identity.RootElement.GetProperty("email").GetString());
            Assert.Equal("Admin", identity.RootElement.GetProperty("role").GetString());

            HttpResponseMessage logout = await factory.Client.PostAsync("/api/auth/logout", null);
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Client.GetAsync("/api/members")).StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies identical invalid credential responses and antiforgery enforcement.</summary>
    [Fact]
    public async Task Login_InvalidCredentialsAreIndistinguishableAndRequireAntiforgery()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        try
        {
            LoginRequest wrong = new(factory.AdminEmail, Guid.NewGuid().ToString("N"));
            HttpResponseMessage withoutToken = await factory.Client.PostAsJsonAsync("/api/auth/login", wrong);
            Assert.Equal(HttpStatusCode.BadRequest, withoutToken.StatusCode);
            await factory.AddAntiforgeryTokenAsync();
            HttpResponseMessage wrongPassword = await factory.Client.PostAsJsonAsync("/api/auth/login", wrong);
            HttpResponseMessage missingEmail = await factory.Client.PostAsJsonAsync(
                "/api/auth/login", new LoginRequest("missing@example.test", Guid.NewGuid().ToString("N")));
            await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
            {
                SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
                await context.Database.ExecuteSqlRawAsync("UPDATE AdminUsers SET IsActive = 0");
            }
            HttpResponseMessage inactive = await factory.Client.PostAsJsonAsync(
                "/api/auth/login", new LoginRequest(factory.AdminEmail, factory.AdminPassword));

            Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, missingEmail.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, inactive.StatusCode);
            string? wrongCode = await ReadCodeAsync(wrongPassword);
            string? missingCode = await ReadCodeAsync(missingEmail);
            string? inactiveCode = await ReadCodeAsync(inactive);
            Assert.Equal(wrongCode, missingCode);
            Assert.Equal(missingCode, inactiveCode);
            Assert.Equal("INVALID_CREDENTIALS", wrongCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies 401 without a cookie, 403 without Admin role and access with Admin.</summary>
    [Fact]
    public async Task Members_RequireAuthenticatedAdminRole()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        try
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Client.GetAsync("/api/members")).StatusCode);
            HttpClient nonAdminClient = CreateNonAdminClient(factory);
            Assert.Equal(HttpStatusCode.Forbidden, (await nonAdminClient.GetAsync("/api/members")).StatusCode);
            await factory.AuthenticateAsync();
            Assert.Equal(HttpStatusCode.OK, (await factory.Client.GetAsync("/api/members")).StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies separation and security attributes of antiforgery cookies.</summary>
    [Fact]
    public async Task Antiforgery_UsesSeparateInternalAndAngularCookies()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        try
        {
            HttpResponseMessage response = await factory.AddAntiforgeryTokenAsync();
            string[] cookies = response.Headers.GetValues("Set-Cookie").ToArray();
            string internalCookie = cookies.Single(value => value.StartsWith("SociedadFomento.Antiforgery=", StringComparison.Ordinal));
            string angularCookie = cookies.Single(value => value.StartsWith($"{AuthenticationConstants.XsrfCookieName}=", StringComparison.Ordinal));

            Assert.Contains("httponly", internalCookie, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("httponly", angularCookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", angularCookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", angularCookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("path=/", angularCookie, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(factory.AdminEmail, angularCookie, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(factory.AdminPassword, angularCookie, StringComparison.Ordinal);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that only the configured number of login attempts is accepted per window.</summary>
    [Fact]
    public async Task Login_IsRateLimitedPerClientIp()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        try
        {
            await factory.AddAntiforgeryTokenAsync();
            LoginRequest invalid = new("missing@example.test", Guid.NewGuid().ToString("N"));
            List<HttpResponseMessage> responses = [];
            for (int attempt = 0; attempt < 6; attempt++)
            {
                responses.Add(await factory.Client.PostAsJsonAsync("/api/auth/login", invalid));
            }

            Assert.All(responses.Take(5), response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
            Assert.Equal(HttpStatusCode.TooManyRequests, responses[5].StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    private static HttpClient CreateNonAdminClient(MemberApiFactory factory)
    {
        IOptionsMonitor<CookieAuthenticationOptions> monitor = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        CookieAuthenticationOptions options = monitor.Get(CookieAuthenticationDefaults.AuthenticationScheme);
        Claim[] claims = [new(ClaimTypes.NameIdentifier, "999"), new(ClaimTypes.Email, "viewer@example.test")];
        AuthenticationTicket ticket = new(new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            CookieAuthenticationDefaults.AuthenticationScheme);
        string protectedTicket = options.TicketDataFormat.Protect(ticket);
        HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Cookie", $"{AuthenticationConstants.CookieName}={protectedTicket}");
        return client;
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        JsonDocument problem = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return problem.RootElement.GetProperty("code").GetString();
    }
}

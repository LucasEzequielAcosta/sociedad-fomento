using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SociedadFomento.Api.Authentication;
using SociedadFomento.Api.Contracts.Authentication;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.Authentication;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Api.Tests;

internal sealed class MemberApiFactory : WebApplicationFactory<Program>
{
    private readonly bool withFeeRate;
    private readonly string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoApiTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";

    internal MemberApiFactory(bool withFeeRate)
    {
        this.withFeeRate = withFeeRate;
    }

    internal FixedClock Clock { get; } = new(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
    internal string AdminEmail { get; } = $"admin-{Guid.NewGuid():N}@example.test";
    internal string AdminPassword { get; } = $"{Guid.NewGuid():N}aA1!";
    internal HttpClient Client { get; private set; } = null!;

    internal async Task InitializeDatabaseAsync()
    {
        Client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.MigrateAsync();
        IAdminPasswordService passwordService = scope.ServiceProvider.GetRequiredService<IAdminPasswordService>();
        AdminUser admin = AdminUser.Create(AdminEmail, Clock.UtcNow, user => passwordService.HashPassword(user, AdminPassword));
        context.AdminUsers.Add(admin);
        await context.SaveChangesAsync();
        if (withFeeRate)
        {
            context.FeeRates.Add(new FeeRate(new DateOnly(2026, 10, 1), 5000m, Clock.UtcNow));
            await context.SaveChangesAsync();
        }
    }

    internal async Task<HttpResponseMessage> AuthenticateAsync(HttpClient? client = null)
    {
        HttpClient targetClient = client ?? Client;
        await AddAntiforgeryTokenAsync(targetClient);
        HttpResponseMessage response = await targetClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(AdminEmail, AdminPassword));
        if (response.IsSuccessStatusCode)
        {
            await AddAntiforgeryTokenAsync(targetClient);
        }
        return response;
    }

    internal async Task<HttpResponseMessage> AddAntiforgeryTokenAsync(HttpClient? client = null)
    {
        HttpClient targetClient = client ?? Client;
        HttpResponseMessage response = await targetClient.GetAsync("/api/auth/antiforgery");
        string cookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith($"{AuthenticationConstants.XsrfCookieName}=", StringComparison.Ordinal));
        string token = Uri.UnescapeDataString(cookie.Split(';')[0].Split('=', 2)[1]);
        targetClient.DefaultRequestHeaders.Remove(AuthenticationConstants.XsrfHeaderName);
        targetClient.DefaultRequestHeaders.Add(AuthenticationConstants.XsrfHeaderName, token);
        return response;
    }

    internal async Task DeleteDatabaseAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.EnsureDeletedAsync();
        Client.Dispose();
        await DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connectionString }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<SociedadFomentoDbContext>();
            services.RemoveAll<DbContextOptions<SociedadFomentoDbContext>>();
            services.AddDbContext<SociedadFomentoDbContext>(options => options.UseSqlServer(connectionString));
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
        });
    }
}

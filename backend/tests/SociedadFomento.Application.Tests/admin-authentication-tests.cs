using System.Reflection;
using SociedadFomento.Application.Authentication;
using SociedadFomento.Application.Authentication.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests administrator login and bootstrap workflows.</summary>
public sealed class AdminAuthenticationTests
{
    private static readonly DateTime CurrentUtc = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Verifies successful login and controlled hash refresh.</summary>
    [Fact]
    public async Task Login_SuccessRehashNeededUpdatesOnlyHash()
    {
        FakeAdminAuthenticationContext context = new();
        AdminUser user = new("admin@example.test", "old-hash", CurrentUtc);
        context.Users.Add(user);
        FakeAdminPasswordService passwordService = new()
        {
            Verification = PasswordVerificationStatus.SuccessRehashNeeded,
            GeneratedHash = "new-hash"
        };
        AdminAuthenticationService service = new(context, passwordService);

        AdminLoginResult result = await service.LoginAsync(" ADMIN@example.test ", Guid.NewGuid().ToString("N"));

        Assert.True(result.Succeeded);
        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Equal(1, context.SaveCount);
        Assert.Equal("admin@example.test", result.Identity!.Email);
    }

    /// <summary>Verifies indistinguishable failure results for invalid login causes.</summary>
    [Fact]
    public async Task Login_InvalidCausesReturnSameResult()
    {
        string suppliedPassword = Guid.NewGuid().ToString("N");
        FakeAdminAuthenticationContext missingContext = new();
        AdminAuthenticationService missingService = new(missingContext, new FakeAdminPasswordService());
        AdminLoginResult missing = await missingService.LoginAsync("missing@example.test", suppliedPassword);

        FakeAdminAuthenticationContext wrongContext = new();
        wrongContext.Users.Add(new AdminUser("admin@example.test", "hash", CurrentUtc));
        AdminAuthenticationService wrongService = new(wrongContext, new FakeAdminPasswordService { Verification = PasswordVerificationStatus.Failed });
        AdminLoginResult wrong = await wrongService.LoginAsync("admin@example.test", suppliedPassword);

        FakeAdminAuthenticationContext inactiveContext = new();
        AdminUser inactiveUser = new("inactive@example.test", "hash", CurrentUtc);
        typeof(AdminUser).GetProperty(nameof(AdminUser.IsActive), BindingFlags.Instance | BindingFlags.Public)!.SetValue(inactiveUser, false);
        inactiveContext.Users.Add(inactiveUser);
        AdminAuthenticationService inactiveService = new(inactiveContext, new FakeAdminPasswordService());
        AdminLoginResult inactive = await inactiveService.LoginAsync("inactive@example.test", suppliedPassword);

        Assert.Equal(missing, wrong);
        Assert.Equal(wrong, inactive);
        Assert.False(missing.Succeeded);
    }

    /// <summary>Verifies bootstrap no-op, validation, creation and idempotency.</summary>
    [Fact]
    public async Task Bootstrap_IsValidatedAndIdempotent()
    {
        FakeAdminAuthenticationContext context = new();
        FakeAdminPasswordService passwordService = new();
        AdminBootstrapService service = new(context, passwordService, new FixedClock(CurrentUtc));
        string password = Guid.NewGuid().ToString("N");

        await service.BootstrapAsync(null, null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.BootstrapAsync("admin@example.test", null));
        await service.BootstrapAsync("Admin@Example.test", password);
        AdminUser created = Assert.Single(context.Users);
        string originalHash = created.PasswordHash;
        await service.BootstrapAsync("admin@example.test", Guid.NewGuid().ToString("N"));

        Assert.Single(context.Users);
        Assert.Equal(originalHash, created.PasswordHash);
        Assert.NotEqual(password, created.PasswordHash);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Authentication;
using SociedadFomento.Application.Authentication.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies administrator hashing, uniqueness and bootstrap using SQL Server.</summary>
public sealed class AdminAuthenticationPersistenceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture fixture;

    /// <summary>Creates the authentication persistence test suite.</summary>
    public AdminAuthenticationPersistenceTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>Verifies concurrent bootstrap remains idempotent across independent contexts.</summary>
    [Fact]
    public async Task Bootstrap_ConcurrentCreationProducesOneValidAdministrator()
    {
        await ClearAdministratorsAsync();
        await using AsyncServiceScope firstScope = fixture.Services.CreateAsyncScope();
        await using AsyncServiceScope secondScope = fixture.Services.CreateAsyncScope();
        AdminBootstrapService first = firstScope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        AdminBootstrapService second = secondScope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        string firstPassword = $"{Guid.NewGuid():N}aA1!";
        string secondPassword = $"{Guid.NewGuid():N}aA1!";

        await Task.WhenAll(
            first.BootstrapAsync("Concurrent@Example.test", firstPassword),
            second.BootstrapAsync(" concurrent@example.test ", secondPassword));

        await using AsyncServiceScope verificationScope = fixture.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = verificationScope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        IAdminPasswordService passwordService = verificationScope.ServiceProvider.GetRequiredService<IAdminPasswordService>();
        AdminUser user = await context.AdminUsers.SingleAsync();
        bool firstValid = passwordService.VerifyHashedPassword(user, firstPassword) == PasswordVerificationStatus.Success;
        bool secondValid = passwordService.VerifyHashedPassword(user, secondPassword) == PasswordVerificationStatus.Success;
        Assert.True(firstValid ^ secondValid);
        Assert.Equal("CONCURRENT@EXAMPLE.TEST", user.NormalizedEmail);
        Assert.True(user.IsActive);
    }

    /// <summary>Verifies real hashing, unique normalized email and idempotent bootstrap.</summary>
    [Fact]
    public async Task Bootstrap_PersistsOnlyHashAndDoesNotOverwriteExistingAdmin()
    {
        await ClearAdministratorsAsync();
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        AdminBootstrapService bootstrap = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        IAdminPasswordService passwordService = scope.ServiceProvider.GetRequiredService<IAdminPasswordService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        string password = $"{Guid.NewGuid():N}aA1!";

        await bootstrap.BootstrapAsync("Admin@Example.test", password);
        context.ChangeTracker.Clear();
        AdminUser user = await context.AdminUsers.SingleAsync();
        string originalHash = user.PasswordHash;
        await bootstrap.BootstrapAsync(" admin@example.test ", Guid.NewGuid().ToString("N"));
        context.ChangeTracker.Clear();
        AdminUser persisted = await context.AdminUsers.SingleAsync();

        Assert.NotEqual(password, persisted.PasswordHash);
        Assert.Equal(PasswordVerificationStatus.Success, passwordService.VerifyHashedPassword(persisted, password));
        Assert.Equal(originalHash, persisted.PasswordHash);
        Assert.Equal(1, await context.AdminUsers.CountAsync());
    }

    private async Task ClearAdministratorsAsync()
    {
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.AdminUsers.ExecuteDeleteAsync();
    }
}

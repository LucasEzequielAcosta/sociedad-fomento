using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Provides an isolated SQL Server LocalDB database for integration tests.</summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";

    internal FixedClock Clock { get; } = new(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
    internal ServiceProvider Services { get; private set; } = null!;

    /// <summary>Creates and migrates the isolated database.</summary>
    public async Task InitializeAsync()
    {
        ServiceCollection services = new();
        services.AddInfrastructure(connectionString);
        services.AddSingleton<IClock>(Clock);
        Services = services.BuildServiceProvider();
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>Deletes the isolated database created by this fixture.</summary>
    public async Task DisposeAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.EnsureDeletedAsync();
        await Services.DisposeAsync();
    }
}

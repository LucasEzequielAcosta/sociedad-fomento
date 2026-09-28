using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies safe bootstrap behavior when the authentication migration is absent.</summary>
public sealed class AdminBootstrapHostedServiceTests
{
    /// <summary>Verifies startup fails safely without creating or migrating the authentication schema.</summary>
    [Fact]
    public async Task StartAsync_WithoutAuthenticationSchemaFailsSafely()
    {
        string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoBootstrapTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
        string email = $"admin-{Guid.NewGuid():N}@example.test";
        string password = $"{Guid.NewGuid():N}aA1!";
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AdminBootstrap:Email"] = email,
            ["AdminBootstrap:Password"] = password
        }).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddInfrastructure(connectionString);
        await using ServiceProvider provider = services.BuildServiceProvider();

        try
        {
            await using AsyncServiceScope scope = provider.CreateAsyncScope();
            SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
            IMigrator migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260928002047_StrengthenFinancialIntegrity");
            IHostedService hostedService = provider.GetServices<IHostedService>().Single();

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                hostedService.StartAsync(CancellationToken.None));

            Assert.Contains("Apply migrations", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(password, exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(email, exception.Message, StringComparison.OrdinalIgnoreCase);
            await using SqlConnection connection = new(connectionString);
            await connection.OpenAsync();
            await using SqlCommand command = new("SELECT OBJECT_ID(N'AdminUsers')", connection);
            Assert.Equal(DBNull.Value, await command.ExecuteScalarAsync());
        }
        finally
        {
            DbContextOptions<SociedadFomentoDbContext> options = new DbContextOptionsBuilder<SociedadFomentoDbContext>()
                .UseSqlServer(connectionString).Options;
            await using SociedadFomentoDbContext cleanup = new(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SociedadFomento.Infrastructure.Persistence;

/// <summary>Creates the database context for Entity Framework design-time tooling.</summary>
public sealed class SociedadFomentoDbContextFactory : IDesignTimeDbContextFactory<SociedadFomentoDbContext>
{
    private const string LocalConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomento;Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>Creates a design-time context without connecting to the database.</summary>
    public SociedadFomentoDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<SociedadFomentoDbContext> options = new();
        options.UseSqlServer(LocalConnectionString);
        return new SociedadFomentoDbContext(options.Options);
    }
}

using Microsoft.EntityFrameworkCore;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Persistence;

/// <summary>Provides access to the Sociedad de Fomento relational data model.</summary>
public sealed class SociedadFomentoDbContext : DbContext
{
    /// <summary>Creates a database context with the supplied options.</summary>
    public SociedadFomentoDbContext(DbContextOptions<SociedadFomentoDbContext> options) : base(options) { }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipPeriod> MembershipPeriods => Set<MembershipPeriod>();
    public DbSet<FeeRate> FeeRates => Set<FeeRate>();
    public DbSet<FeeObligation> FeeObligations => Set<FeeObligation>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<AccountingEntry> AccountingEntries => Set<AccountingEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SociedadFomentoDbContext).Assembly);
    }
}

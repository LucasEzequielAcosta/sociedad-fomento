using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Accounting;
using SociedadFomento.Application.Accounting.Models;
using SociedadFomento.Application.Dashboard;
using SociedadFomento.Application.Dashboard.Dto;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies dashboard aggregation queries against an isolated SQL Server database.</summary>
public sealed class DashboardPersistenceTests : IAsyncLifetime
{
    private readonly string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoDashboardTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
    private readonly FixedClock clock = new(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
    private ServiceProvider services = null!;

    /// <summary>Creates and migrates the isolated database.</summary>
    public async Task InitializeAsync()
    {
        ServiceCollection serviceCollection = new();
        serviceCollection.AddInfrastructure(connectionString);
        serviceCollection.AddSingleton<SociedadFomento.Application.Abstractions.IClock>(clock);
        services = serviceCollection.BuildServiceProvider();
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>Deletes the isolated database.</summary>
    public async Task DisposeAsync()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.EnsureDeletedAsync();
        await services.DisposeAsync();
    }

    /// <summary>Verifies that the dashboard counts members, unpaid obligations and current-month economy.</summary>
    [Fact]
    public async Task Get_AggregatesMembersObligationsAndEconomy()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        DashboardService service = scope.ServiceProvider.GetRequiredService<DashboardService>();
        AccountingEntryService accountingEntryService = scope.ServiceProvider.GetRequiredService<AccountingEntryService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();

        FeeRate septemberRate = new(new DateOnly(2026, 9, 1), 5000m, clock.UtcNow);
        FeeRate octoberRate = new(new DateOnly(2026, 10, 1), 7000m, clock.UtcNow);
        context.FeeRates.AddRange(septemberRate, octoberRate);

        Member pendingMember = CreateMember("11111111");
        Member overdueMember = CreateMember("22222222");
        Member inactiveMember = CreateMember("33333333");
        inactiveMember.Deactivate(new DateOnly(2026, 10, 1));
        context.Members.AddRange(pendingMember, overdueMember, inactiveMember);
        await context.SaveChangesAsync();

        context.FeeObligations.AddRange(
            new FeeObligation(pendingMember, pendingMember.MembershipPeriods.Single(), octoberRate, new DateOnly(2026, 10, 1), clock.UtcNow),
            new FeeObligation(pendingMember, pendingMember.MembershipPeriods.Single(), octoberRate, new DateOnly(2026, 11, 1), clock.UtcNow),
            new FeeObligation(overdueMember, overdueMember.MembershipPeriods.Single(), septemberRate, new DateOnly(2026, 9, 1), clock.UtcNow),
            new FeeObligation(inactiveMember, inactiveMember.MembershipPeriods.Single(), septemberRate, new DateOnly(2026, 9, 1), clock.UtcNow));

        Member payer = CreateMember("44444444");
        FeeObligation paidObligation = new(payer, payer.MembershipPeriods.Single(), septemberRate, new DateOnly(2026, 9, 1), clock.UtcNow);
        Payment payment = new(payer, new DateOnly(2026, 9, 15), paidObligation.Amount, PaymentMethod.Cash, [paidObligation], clock.UtcNow);
        AccountingEntry.CreateForPayment(payment, "Pago de cuota", clock.UtcNow);
        context.Payments.Add(payment);
        await accountingEntryService.CreateManualAsync(
            new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 3000m), clock.UtcNow);
        await context.SaveChangesAsync();

        DashboardDto dashboard = await service.GetAsync();

        Assert.Equal(3, dashboard.Members.Active);
        Assert.Equal(1, dashboard.Members.Inactive);
        Assert.Equal(1, dashboard.Obligations.Pending);
        Assert.Equal(2, dashboard.Obligations.Overdue);
        Assert.Equal(17000m, dashboard.Obligations.TotalDebt);
        Assert.Equal(3000m, dashboard.Economy.TotalIncome);
        Assert.Equal(0m, dashboard.Economy.TotalExpense);
        Assert.Equal(3000m, dashboard.Economy.Balance);
    }

    private static Member CreateMember(string dni) => new(
        dni, "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "+54 223 555-0000", null, new DateOnly(2026, 9, 1));
}

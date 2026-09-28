using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Payments;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies relational integrity and transactional payment cancellation against SQL Server.</summary>
public sealed class DataIntegrityTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture fixture;

    /// <summary>Creates the integration test suite.</summary>
    public DataIntegrityTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>Verifies that an obligation cannot reference another member's activity period.</summary>
    [Fact]
    public async Task FeeObligation_RejectsMembershipPeriodFromAnotherMember()
    {
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        FeeObligation first = await PersistenceTestData.AddObligationAsync(context, "30000001", new DateOnly(2030, 1, 1));
        FeeObligation second = await PersistenceTestData.AddObligationAsync(context, "30000002", new DateOnly(2030, 2, 1));

        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO FeeObligations (MemberId, MembershipPeriodId, FeeRateId, Period, Amount, CreatedAtUtc)
            VALUES ({first.MemberId}, {second.MembershipPeriodId}, {second.FeeRateId}, {new DateOnly(2030, 3, 1)}, {second.Amount}, {PersistenceTestData.CreatedAtUtc})
            """));
    }

    /// <summary>Verifies that an allocation cannot cross member boundaries.</summary>
    [Fact]
    public async Task PaymentAllocation_RejectsObligationFromAnotherMember()
    {
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        Payment payment = await PersistenceTestData.AddPaymentAsync(context, "30000003", new DateOnly(2030, 4, 1), new DateOnly(2030, 4, 5));
        FeeObligation other = await PersistenceTestData.AddObligationAsync(context, "30000004", new DateOnly(2030, 5, 1));

        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO PaymentAllocations (PaymentId, FeeObligationId, MemberId)
            VALUES ({payment.Id}, {other.Id}, {payment.MemberId})
            """));
    }

    /// <summary>Verifies that an obligation snapshot must match its referenced fee rate.</summary>
    [Fact]
    public async Task FeeObligation_RejectsAmountDifferentFromFeeRate()
    {
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        FeeObligation obligation = await PersistenceTestData.AddObligationAsync(context, "30000005", new DateOnly(2030, 6, 1));
        decimal invalidAmount = obligation.Amount - 1m;

        await Assert.ThrowsAsync<SqlException>(() => context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO FeeObligations (MemberId, MembershipPeriodId, FeeRateId, Period, Amount, CreatedAtUtc)
            VALUES ({obligation.MemberId}, {obligation.MembershipPeriodId}, {obligation.FeeRateId}, {new DateOnly(2030, 7, 1)}, {invalidAmount}, {PersistenceTestData.CreatedAtUtc})
            """));
    }

    /// <summary>Verifies that reactivation leaves an existing obligation snapshot unchanged.</summary>
    [Fact]
    public async Task Reactivation_PreservesExistingObligationSnapshot()
    {
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        FeeObligation obligation = await PersistenceTestData.AddObligationAsync(context, "30000006", new DateOnly(2030, 8, 1));
        long membershipPeriodId = obligation.MembershipPeriodId;
        long feeRateId = obligation.FeeRateId;
        decimal amount = obligation.Amount;
        obligation.Member.Deactivate(new DateOnly(2030, 8, 10));
        obligation.Member.Reactivate(new DateOnly(2030, 8, 20));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        FeeObligation persisted = await context.FeeObligations.SingleAsync(item => item.Id == obligation.Id);

        Assert.Equal(membershipPeriodId, persisted.MembershipPeriodId);
        Assert.Equal(feeRateId, persisted.FeeRateId);
        Assert.Equal(amount, persisted.Amount);
    }

    /// <summary>Verifies that cancellation rolls back when the automatic income is missing.</summary>
    [Fact]
    public async Task CancelPayment_RollsBackWhenAccountingEntryIsMissing()
    {
        long paymentId;
        await using (AsyncServiceScope setupScope = fixture.Services.CreateAsyncScope())
        {
            SociedadFomentoDbContext setupContext = setupScope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
            FeeObligation obligation = await PersistenceTestData.AddObligationAsync(
                setupContext, "30000008", new DateOnly(2030, 10, 1));
            Payment payment = new(
                obligation.Member, new DateOnly(2030, 10, 5), obligation.Amount, PaymentMethod.Cash, [obligation], PersistenceTestData.CreatedAtUtc);
            setupContext.Payments.Add(payment);
            await setupContext.SaveChangesAsync();
            paymentId = payment.Id;
        }

        await using (AsyncServiceScope serviceScope = fixture.Services.CreateAsyncScope())
        {
            PaymentCancellationService service = serviceScope.ServiceProvider.GetRequiredService<PaymentCancellationService>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelAsync(paymentId));
        }

        await using AsyncServiceScope assertionScope = fixture.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = assertionScope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        Payment persisted = await context.Payments.Include(item => item.PaymentAllocations).SingleAsync(item => item.Id == paymentId);
        Assert.Equal(PaymentStatus.Active, persisted.Status);
        Assert.All(persisted.PaymentAllocations, allocation => Assert.Null(allocation.ReleasedAtUtc));
    }

    /// <summary>Verifies atomic cancellation of payment, allocations and automatic income.</summary>
    [Fact]
    public async Task CancelPayment_CancelsAllEffectsAndPreservesHistory()
    {
        long paymentId;
        await using (AsyncServiceScope setupScope = fixture.Services.CreateAsyncScope())
        {
            SociedadFomentoDbContext setupContext = setupScope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
            Payment payment = await PersistenceTestData.AddPaymentAsync(
                setupContext, "30000007", new DateOnly(2030, 9, 1), new DateOnly(2030, 9, 5));
            paymentId = payment.Id;
        }

        await using (AsyncServiceScope serviceScope = fixture.Services.CreateAsyncScope())
        {
            PaymentCancellationService service = serviceScope.ServiceProvider.GetRequiredService<PaymentCancellationService>();
            await service.CancelAsync(paymentId);
        }

        await using AsyncServiceScope assertionScope = fixture.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = assertionScope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        Payment persisted = await context.Payments.Include(item => item.PaymentAllocations)
            .Include(item => item.AccountingEntry).SingleAsync(item => item.Id == paymentId);

        Assert.Equal(PaymentStatus.Cancelled, persisted.Status);
        Assert.All(persisted.PaymentAllocations, allocation => Assert.Equal(fixture.Clock.UtcNow, allocation.ReleasedAtUtc));
        Assert.Equal(AccountingEntryStatus.Cancelled, persisted.AccountingEntry!.Status);
        Assert.Equal(fixture.Clock.UtcNow, persisted.AccountingEntry.CancelledAtUtc);
        Assert.Equal(persisted.PaymentDate, persisted.AccountingEntry.EntryDate);
        Assert.Equal(5000m, persisted.AccountingEntry.Amount);
        Assert.Equal("Pago de cuota", persisted.AccountingEntry.Concept);
        Assert.NotEmpty(persisted.PaymentAllocations);
    }
}

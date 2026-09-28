using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Domain.Tests;

/// <summary>Tests payment and allocation behavior.</summary>
public sealed class PaymentTests
{
    /// <summary>Verifies date-based obligation statuses and complete payment allocation.</summary>
    [Fact]
    public void Payment_MarksCompleteObligationAsPaid()
    {
        Member member = TestData.CreateMember();
        FeeObligation obligation = TestData.CreateObligation(member, new DateOnly(2026, 10, 1));
        Payment payment = new(member, new DateOnly(2026, 9, 20), 5000m, PaymentMethod.Cash, [obligation], TestData.CreatedAtUtc);

        FeeObligationStatus status = obligation.GetStatus(new DateOnly(2026, 9, 20));

        Assert.Equal(FeeObligationStatus.Paid, status);
        Assert.Single(payment.PaymentAllocations);
    }

    /// <summary>Verifies the complete date-based transition of an unpaid obligation.</summary>
    [Fact]
    public void GetStatus_TransitionsFromFutureToPendingToOverdue()
    {
        Member member = TestData.CreateMember();
        FeeObligation obligation = TestData.CreateObligation(member, new DateOnly(2026, 10, 1));

        Assert.Equal(FeeObligationStatus.Future, obligation.GetStatus(new DateOnly(2026, 9, 30)));
        Assert.Equal(FeeObligationStatus.Pending, obligation.GetStatus(new DateOnly(2026, 10, 15)));
        Assert.Equal(FeeObligationStatus.Overdue, obligation.GetStatus(new DateOnly(2026, 11, 1)));
    }

    /// <summary>Verifies that cancellation releases allocations while retaining their history.</summary>
    [Fact]
    public void Cancel_ReleasesAllocationAndRestoresCalculatedStatus()
    {
        Member member = TestData.CreateMember();
        FeeObligation obligation = TestData.CreateObligation(member, new DateOnly(2026, 10, 1));
        Payment payment = new(member, new DateOnly(2026, 9, 20), 5000m, PaymentMethod.Cash, [obligation], TestData.CreatedAtUtc);
        DateTime cancelledAtUtc = TestData.CreatedAtUtc.AddDays(1);

        payment.Cancel(cancelledAtUtc);

        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        Assert.Equal(cancelledAtUtc, payment.PaymentAllocations.Single().ReleasedAtUtc);
        Assert.Equal(FeeObligationStatus.Future, obligation.GetStatus(new DateOnly(2026, 9, 20)));
    }

    /// <summary>Verifies that a payment rejects obligations owned by another member.</summary>
    [Fact]
    public void Constructor_RejectsObligationFromAnotherMember()
    {
        Member member = TestData.CreateMember();
        Member otherMember = TestData.CreateMember(new DateOnly(2026, 10, 1));
        FeeObligation obligation = TestData.CreateObligation(otherMember, new DateOnly(2026, 10, 1));

        Assert.Throws<ArgumentException>(() =>
            new Payment(member, new DateOnly(2026, 9, 20), 5000m, PaymentMethod.Cash, [obligation], TestData.CreatedAtUtc));
    }

    /// <summary>Verifies that partial payment amounts are rejected.</summary>
    [Fact]
    public void Constructor_RejectsPartialPayment()
    {
        Member member = TestData.CreateMember();
        FeeObligation obligation = TestData.CreateObligation(member, new DateOnly(2026, 9, 1));

        Assert.Throws<ArgumentException>(() =>
            new Payment(member, new DateOnly(2026, 9, 20), 2500m, PaymentMethod.Cash, [obligation], TestData.CreatedAtUtc));
    }
}

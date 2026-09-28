using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Domain.Entities;

/// <summary>Represents an immutable monthly fee obligation for a member.</summary>
public sealed class FeeObligation
{
    private readonly List<PaymentAllocation> paymentAllocations = [];

    private FeeObligation() { }

    /// <summary>Creates a monthly obligation using a snapshot of the applicable fee rate.</summary>
    public FeeObligation(Member member, MembershipPeriod membershipPeriod, FeeRate feeRate, DateOnly period, DateTime createdAtUtc)
    {
        if (!ReferenceEquals(membershipPeriod.Member, member))
        {
            throw new ArgumentException("The membership period must belong to the obligation member.", nameof(membershipPeriod));
        }

        Member = member;
        MembershipPeriod = membershipPeriod;
        FeeRate = feeRate;
        Period = new DateOnly(period.Year, period.Month, 1);
        Amount = feeRate.Amount;
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; private set; }
    public long MemberId { get; private set; }
    public long MembershipPeriodId { get; private set; }
    public long FeeRateId { get; private set; }
    public DateOnly Period { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public Member Member { get; private set; } = null!;
    public MembershipPeriod MembershipPeriod { get; private set; } = null!;
    public FeeRate FeeRate { get; private set; } = null!;
    public IReadOnlyCollection<PaymentAllocation> PaymentAllocations => paymentAllocations;

    internal void AddAllocation(PaymentAllocation allocation) => paymentAllocations.Add(allocation);

    /// <summary>Calculates the status using the supplied current date.</summary>
    public FeeObligationStatus GetStatus(DateOnly currentDate)
    {
        if (paymentAllocations.Any(allocation => !allocation.ReleasedAtUtc.HasValue))
        {
            return FeeObligationStatus.Paid;
        }

        DateOnly currentMonth = new(currentDate.Year, currentDate.Month, 1);
        return Period > currentMonth
            ? FeeObligationStatus.Future
            : Period == currentMonth ? FeeObligationStatus.Pending : FeeObligationStatus.Overdue;
    }
}

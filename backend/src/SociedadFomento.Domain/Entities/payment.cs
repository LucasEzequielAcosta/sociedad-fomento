using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Domain.Entities;

/// <summary>Represents a payment allocated to one or more complete fee obligations.</summary>
public sealed class Payment
{
    private readonly List<PaymentAllocation> paymentAllocations = [];

    private Payment() { }

    /// <summary>Creates an active payment and its complete obligation allocations.</summary>
    public Payment(Member member, DateOnly paymentDate, decimal amount, PaymentMethod method, IReadOnlyCollection<FeeObligation> obligations, DateTime createdAtUtc)
    {
        EnsureValidObligations(member, amount, obligations);
        Member = member;
        PaymentDate = paymentDate;
        Amount = amount;
        Method = method;
        Status = PaymentStatus.Active;
        CreatedAtUtc = createdAtUtc;
        AddAllocations(obligations);
    }

    public long Id { get; private set; }
    public long MemberId { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public Member Member { get; private set; } = null!;
    public IReadOnlyCollection<PaymentAllocation> PaymentAllocations => paymentAllocations;
    public AccountingEntry? AccountingEntry { get; private set; }

    /// <summary>Cancels this payment and releases all its active allocations.</summary>
    public void Cancel(DateTime cancelledAtUtc)
    {
        if (Status == PaymentStatus.Cancelled)
        {
            throw new InvalidOperationException("The payment is already cancelled.");
        }

        paymentAllocations.Where(allocation => !allocation.ReleasedAtUtc.HasValue)
            .ToList()
            .ForEach(allocation => allocation.Release(cancelledAtUtc));
        Status = PaymentStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
    }

    internal void SetAccountingEntry(AccountingEntry accountingEntry) => AccountingEntry = accountingEntry;

    private void AddAllocations(IEnumerable<FeeObligation> obligations)
    {
        foreach (FeeObligation obligation in obligations)
        {
            PaymentAllocation allocation = new(this, obligation);
            paymentAllocations.Add(allocation);
            obligation.AddAllocation(allocation);
        }
    }

    private static void EnsureValidObligations(Member member, decimal amount, IReadOnlyCollection<FeeObligation> obligations)
    {
        bool hasInvalidMember = obligations.Any(obligation => !ReferenceEquals(obligation.Member, member));
        bool hasInvalidAmount = obligations.Count == 0 || amount <= 0 || obligations.Sum(obligation => obligation.Amount) != amount;
        if (hasInvalidMember || hasInvalidAmount)
        {
            throw new ArgumentException("The payment must match complete obligations belonging to the member.", nameof(obligations));
        }
    }
}

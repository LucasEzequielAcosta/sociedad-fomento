namespace SociedadFomento.Domain.Entities;

/// <summary>Represents the historical allocation of a payment to a complete fee obligation.</summary>
public sealed class PaymentAllocation
{
    private PaymentAllocation() { }

    internal PaymentAllocation(Payment payment, FeeObligation feeObligation)
    {
        if (!ReferenceEquals(payment.Member, feeObligation.Member))
        {
            throw new ArgumentException("The payment and obligation must belong to the same member.", nameof(feeObligation));
        }

        Payment = payment;
        FeeObligation = feeObligation;
        MemberId = payment.MemberId;
    }

    public long Id { get; private set; }
    public long PaymentId { get; private set; }
    public long FeeObligationId { get; private set; }
    public long MemberId { get; private set; }
    public DateTime? ReleasedAtUtc { get; private set; }
    public Payment Payment { get; private set; } = null!;
    public FeeObligation FeeObligation { get; private set; } = null!;

    /// <summary>Releases this allocation while retaining its history.</summary>
    public void Release(DateTime releasedAtUtc)
    {
        if (ReleasedAtUtc.HasValue)
        {
            throw new InvalidOperationException("The allocation is already released.");
        }

        ReleasedAtUtc = releasedAtUtc;
    }
}

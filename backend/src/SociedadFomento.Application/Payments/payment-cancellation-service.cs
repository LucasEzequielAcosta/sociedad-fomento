using SociedadFomento.Application.Abstractions;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Payments;

/// <summary>Coordinates atomic cancellation of payments and their accounting effects.</summary>
public sealed class PaymentCancellationService
{
    private readonly IPaymentCancellationContext context;
    private readonly IClock clock;

    /// <summary>Creates the payment cancellation service.</summary>
    public PaymentCancellationService(IPaymentCancellationContext context, IClock clock)
    {
        this.context = context;
        this.clock = clock;
    }

    /// <summary>Cancels a payment, releases allocations and cancels its automatic income atomically.</summary>
    public Task CancelAsync(long paymentId, CancellationToken cancellationToken = default)
    {
        return context.ExecuteInTransactionAsync(
            transactionToken => CancelWithinTransactionAsync(paymentId, transactionToken), cancellationToken);
    }

    private async Task CancelWithinTransactionAsync(long paymentId, CancellationToken cancellationToken)
    {
        Payment payment = await context.FindPaymentWithDetailsAsync(paymentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Payment {paymentId} was not found.");
        AccountingEntry entry = payment.AccountingEntry
            ?? throw new InvalidOperationException("The payment has no automatic accounting entry.");
        DateTime cancelledAtUtc = clock.UtcNow;
        payment.Cancel(cancelledAtUtc);
        entry.Cancel(cancelledAtUtc);
        await context.SaveChangesAsync(cancellationToken);
    }
}

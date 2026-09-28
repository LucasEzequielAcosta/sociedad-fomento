using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Abstractions;

/// <summary>Defines persistence operations required to cancel a payment atomically.</summary>
public interface IPaymentCancellationContext
{
    /// <summary>Loads a payment with its allocations and automatic accounting entry.</summary>
    Task<Payment?> FindPaymentWithDetailsAsync(long paymentId, CancellationToken cancellationToken);

    /// <summary>Persists tracked changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Executes an operation within a database transaction.</summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}

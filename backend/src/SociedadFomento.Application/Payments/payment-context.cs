using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Payments;

/// <summary>Defines persistence operations required to register a payment atomically.</summary>
public interface IPaymentContext
{
    Task<Member?> FindMemberAsync(long memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FeeObligation>> FindObligationsAsync(long memberId, IReadOnlyCollection<long> obligationIds, CancellationToken cancellationToken);
    Task<bool> HasActiveAllocationAsync(long feeObligationId, CancellationToken cancellationToken);
    void AddPayment(Payment payment);
    void AddAccountingEntry(AccountingEntry accountingEntry);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}

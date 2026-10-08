using SociedadFomento.Application.Payments;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

internal sealed class FakePaymentContext : IPaymentContext
{
    internal List<Member> Members { get; } = [];
    internal List<FeeObligation> Obligations { get; } = [];
    internal List<Payment> Payments { get; } = [];
    internal List<AccountingEntry> AccountingEntries { get; } = [];

    public Task<Member?> FindMemberAsync(long memberId, CancellationToken cancellationToken) =>
        Task.FromResult(Members.SingleOrDefault(member => member.Id == memberId));

    public Task<IReadOnlyList<FeeObligation>> FindObligationsAsync(
        long memberId, IReadOnlyCollection<long> obligationIds, CancellationToken cancellationToken)
    {
        List<FeeObligation> obligations = Obligations
            .Where(obligation => obligation.MemberId == memberId && obligationIds.Contains(obligation.Id))
            .ToList();
        return Task.FromResult<IReadOnlyList<FeeObligation>>(obligations);
    }

    public Task<bool> HasActiveAllocationAsync(long feeObligationId, CancellationToken cancellationToken) =>
        Task.FromResult(Obligations.Any(obligation =>
            obligation.Id == feeObligationId &&
            obligation.PaymentAllocations.Any(allocation => !allocation.ReleasedAtUtc.HasValue)));

    public void AddPayment(Payment payment) => Payments.Add(payment);

    public void AddAccountingEntry(AccountingEntry accountingEntry) => AccountingEntries.Add(accountingEntry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        operation(cancellationToken);
}

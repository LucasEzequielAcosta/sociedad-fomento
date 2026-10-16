using SociedadFomento.Application.Payments;
using SociedadFomento.Application.Payments.Dto;
using SociedadFomento.Application.Payments.Models;
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

    public Task<IReadOnlyList<PaymentListItemDto>> GetPaymentsAsync(PaymentFilter filter, CancellationToken cancellationToken)
    {
        IEnumerable<Payment> query = Payments;
        if (filter.MemberId is not null)
        {
            query = query.Where(payment => payment.MemberId == filter.MemberId.Value);
        }

        if (filter.From is not null)
        {
            query = query.Where(payment => payment.PaymentDate >= filter.From.Value);
        }

        if (filter.To is not null)
        {
            query = query.Where(payment => payment.PaymentDate <= filter.To.Value);
        }

        if (filter.Method is not null)
        {
            query = query.Where(payment => payment.Method == filter.Method.Value);
        }

        return Task.FromResult<IReadOnlyList<PaymentListItemDto>>(query
            .OrderByDescending(payment => payment.PaymentDate).ThenByDescending(payment => payment.Id)
            .Select(payment => new PaymentListItemDto(
                payment.Id, payment.MemberId, payment.Member.Dni, payment.Member.FirstName, payment.Member.LastName,
                payment.PaymentDate, payment.Amount, payment.Method, payment.Status,
                payment.PaymentAllocations.Select(allocation => allocation.FeeObligation.Period).ToList()))
            .ToList());
    }

    public void AddPayment(Payment payment) => Payments.Add(payment);

    public void AddAccountingEntry(AccountingEntry accountingEntry) => AccountingEntries.Add(accountingEntry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        operation(cancellationToken);
}

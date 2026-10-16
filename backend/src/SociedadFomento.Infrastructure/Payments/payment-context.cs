using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SociedadFomento.Application.Payments;
using SociedadFomento.Application.Payments.Dto;
using SociedadFomento.Application.Payments.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Payments;

internal sealed class PaymentContext : IPaymentContext
{
    private readonly SociedadFomentoDbContext dbContext;

    public PaymentContext(SociedadFomentoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Member?> FindMemberAsync(long memberId, CancellationToken cancellationToken) =>
        dbContext.Members.SingleOrDefaultAsync(member => member.Id == memberId, cancellationToken);

    public async Task<IReadOnlyList<FeeObligation>> FindObligationsAsync(
        long memberId, IReadOnlyCollection<long> obligationIds, CancellationToken cancellationToken)
    {
        List<FeeObligation> obligations = await dbContext.FeeObligations
            .Include(obligation => obligation.Member)
            .Where(obligation => obligation.MemberId == memberId && obligationIds.Contains(obligation.Id))
            .ToListAsync(cancellationToken);
        return obligations;
    }

    public Task<bool> HasActiveAllocationAsync(long feeObligationId, CancellationToken cancellationToken) =>
        dbContext.PaymentAllocations.AnyAsync(
            allocation => allocation.FeeObligationId == feeObligationId && !allocation.ReleasedAtUtc.HasValue,
            cancellationToken);

    public async Task<IReadOnlyList<PaymentListItemDto>> GetPaymentsAsync(PaymentFilter filter, CancellationToken cancellationToken)
    {
        IQueryable<Payment> query = dbContext.Payments.AsNoTracking();
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

        List<PaymentListItemDto> payments = await query
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.Id)
            .Select(payment => new PaymentListItemDto(
                payment.Id,
                payment.MemberId,
                payment.Member.Dni,
                payment.Member.FirstName,
                payment.Member.LastName,
                payment.PaymentDate,
                payment.Amount,
                payment.Method,
                payment.Status,
                payment.PaymentAllocations.Select(allocation => allocation.FeeObligation.Period).ToList()))
            .ToListAsync(cancellationToken);
        return payments;
    }

    public void AddPayment(Payment payment) => dbContext.Payments.Add(payment);

    public void AddAccountingEntry(AccountingEntry accountingEntry) => dbContext.AccountingEntries.Add(accountingEntry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            T result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

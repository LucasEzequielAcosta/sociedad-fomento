using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SociedadFomento.Application.Payments;
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

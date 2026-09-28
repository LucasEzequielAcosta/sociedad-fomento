using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Payments;

internal sealed class PaymentCancellationContext : IPaymentCancellationContext
{
    private readonly SociedadFomentoDbContext dbContext;

    public PaymentCancellationContext(SociedadFomentoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Payment?> FindPaymentWithDetailsAsync(long paymentId, CancellationToken cancellationToken)
    {
        return dbContext.Payments
            .Include(payment => payment.PaymentAllocations)
            .Include(payment => payment.AccountingEntry)
            .SingleOrDefaultAsync(payment => payment.Id == paymentId, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

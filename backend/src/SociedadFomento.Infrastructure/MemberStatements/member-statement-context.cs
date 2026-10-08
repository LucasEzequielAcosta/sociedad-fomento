using Microsoft.EntityFrameworkCore;
using SociedadFomento.Application.MemberStatements;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.MemberStatements;

internal sealed class MemberStatementContext : IMemberStatementContext
{
    private readonly SociedadFomentoDbContext dbContext;

    public MemberStatementContext(SociedadFomentoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Member?> GetMemberAsync(long memberId, CancellationToken cancellationToken) =>
        dbContext.Members.SingleOrDefaultAsync(member => member.Id == memberId, cancellationToken);

    public async Task<IReadOnlyList<FeeObligation>> GetObligationsAsync(long memberId, CancellationToken cancellationToken)
    {
        List<FeeObligation> obligations = await dbContext.FeeObligations
            .AsNoTracking()
            .Include(obligation => obligation.PaymentAllocations)
            .Where(obligation => obligation.MemberId == memberId)
            .OrderBy(obligation => obligation.Period)
            .ToListAsync(cancellationToken);
        return obligations;
    }

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(long memberId, CancellationToken cancellationToken)
    {
        List<Payment> payments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.MemberId == memberId)
            .OrderByDescending(payment => payment.PaymentDate)
            .ToListAsync(cancellationToken);
        return payments;
    }
}

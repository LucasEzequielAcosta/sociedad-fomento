using Microsoft.EntityFrameworkCore;
using SociedadFomento.Application.Dashboard;
using SociedadFomento.Application.Dashboard.Dto;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Dashboard;

internal sealed class DashboardContext : IDashboardContext
{
    private readonly SociedadFomentoDbContext dbContext;

    public DashboardContext(SociedadFomentoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<DashboardMembersDto> GetMemberCountsAsync(CancellationToken cancellationToken)
    {
        List<MemberCount> counts = await dbContext.Members.AsNoTracking()
            .GroupBy(member => member.Status)
            .Select(group => new MemberCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);
        int active = counts.Where(count => count.Status == MemberStatus.Active).Sum(count => count.Count);
        int inactive = counts.Where(count => count.Status == MemberStatus.Inactive).Sum(count => count.Count);
        return new DashboardMembersDto(active, inactive);
    }

    public async Task<DashboardObligationsDto> GetObligationTotalsAsync(DateOnly currentMonth, CancellationToken cancellationToken)
    {
        List<ObligationTotal> totals = await dbContext.FeeObligations.AsNoTracking()
            .Where(obligation => obligation.Period <= currentMonth
                && !obligation.PaymentAllocations.Any(allocation => allocation.ReleasedAtUtc == null))
            .GroupBy(obligation => obligation.Period < currentMonth)
            .Select(group => new ObligationTotal(group.Key, group.Count(), group.Sum(obligation => obligation.Amount)))
            .ToListAsync(cancellationToken);

        ObligationTotal overdue = totals.SingleOrDefault(total => total.IsOverdue) ?? new ObligationTotal(true, 0, 0m);
        ObligationTotal pending = totals.SingleOrDefault(total => !total.IsOverdue) ?? new ObligationTotal(false, 0, 0m);
        return new DashboardObligationsDto(pending.Count, overdue.Count, pending.Amount + overdue.Amount);
    }

    private sealed record MemberCount(MemberStatus Status, int Count);

    private sealed record ObligationTotal(bool IsOverdue, int Count, decimal Amount);
}

using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.Accounting;
using SociedadFomento.Application.Accounting.Dto;
using SociedadFomento.Application.Accounting.Models;
using SociedadFomento.Application.Dashboard.Dto;

namespace SociedadFomento.Application.Dashboard;

/// <summary>Coordinates the administrative dashboard summary.</summary>
public sealed class DashboardService
{
    private readonly IDashboardContext context;
    private readonly AccountingEntryService accountingEntryService;
    private readonly IClock clock;

    /// <summary>Creates the dashboard application service.</summary>
    public DashboardService(IDashboardContext context, AccountingEntryService accountingEntryService, IClock clock)
    {
        this.context = context;
        this.accountingEntryService = accountingEntryService;
        this.clock = clock;
    }

    /// <summary>Returns member, obligation and current-month economy totals.</summary>
    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        DateOnly today = clock.Today;
        DateOnly currentMonth = new(today.Year, today.Month, 1);
        DashboardMembersDto members = await context.GetMemberCountsAsync(cancellationToken);
        DashboardObligationsDto obligations = await context.GetObligationTotalsAsync(currentMonth, cancellationToken);
        AccountingSummaryDto economy = await accountingEntryService.GetSummaryAsync(
            new GetAccountingSummaryModel(AccountingSummaryPeriod.CurrentMonth, null, null), cancellationToken);
        return new DashboardDto(members, obligations, economy);
    }
}

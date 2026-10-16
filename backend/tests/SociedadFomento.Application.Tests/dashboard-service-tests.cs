using SociedadFomento.Application.Accounting;
using SociedadFomento.Application.Dashboard;
using SociedadFomento.Application.Dashboard.Dto;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests dashboard aggregation coordination.</summary>
public sealed class DashboardServiceTests
{
    private static readonly DateTime NowUtc = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Verifies that the dashboard combines member, obligation and economy totals.</summary>
    [Fact]
    public async Task Get_ReturnsCombinedSummary()
    {
        FakeDashboardContext dashboardContext = new()
        {
            Members = new DashboardMembersDto(10, 2),
            Obligations = new DashboardObligationsDto(3, 4, 35000m)
        };
        FakeAccountingEntryContext accountingContext = new();
        accountingContext.Entries.Add(AccountingEntry.CreateManual(
            new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 3000m, NowUtc));
        accountingContext.Entries.Add(AccountingEntry.CreateManual(
            new DateOnly(2026, 10, 6), AccountingEntryType.Expense, "Limpieza", 1000m, NowUtc));
        DashboardService service = new(dashboardContext, new AccountingEntryService(accountingContext, new FixedClock(NowUtc)), new FixedClock(NowUtc));

        DashboardDto dashboard = await service.GetAsync();

        Assert.Equal(10, dashboard.Members.Active);
        Assert.Equal(2, dashboard.Members.Inactive);
        Assert.Equal(3, dashboard.Obligations.Pending);
        Assert.Equal(4, dashboard.Obligations.Overdue);
        Assert.Equal(35000m, dashboard.Obligations.TotalDebt);
        Assert.Equal(new DateOnly(2026, 10, 1), dashboard.Economy.From);
        Assert.Equal(new DateOnly(2026, 10, 31), dashboard.Economy.To);
        Assert.Equal(3000m, dashboard.Economy.TotalIncome);
        Assert.Equal(1000m, dashboard.Economy.TotalExpense);
        Assert.Equal(2000m, dashboard.Economy.Balance);
    }
}

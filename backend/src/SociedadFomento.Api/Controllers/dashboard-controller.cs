using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SociedadFomento.Api.Contracts.Dashboard;
using SociedadFomento.Application.Dashboard;
using SociedadFomento.Application.Dashboard.Dto;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes the administrative dashboard summary.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService dashboardService;

    /// <summary>Creates the dashboard controller.</summary>
    public DashboardController(DashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    /// <summary>Returns member, obligation and current-month economy totals.</summary>
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken cancellationToken)
    {
        DashboardDto dashboard = await dashboardService.GetAsync(cancellationToken);
        return Ok(new DashboardResponse(
            new DashboardMembersResponse(dashboard.Members.Active, dashboard.Members.Inactive),
            new DashboardObligationsResponse(dashboard.Obligations.Pending, dashboard.Obligations.Overdue, dashboard.Obligations.TotalDebt),
            new DashboardEconomyResponse(
                dashboard.Economy.From, dashboard.Economy.To,
                dashboard.Economy.TotalIncome, dashboard.Economy.TotalExpense, dashboard.Economy.Balance)));
    }
}

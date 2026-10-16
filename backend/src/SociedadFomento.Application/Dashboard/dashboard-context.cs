using SociedadFomento.Application.Dashboard.Dto;

namespace SociedadFomento.Application.Dashboard;

/// <summary>Defines dashboard aggregation queries required by the application service.</summary>
public interface IDashboardContext
{
    /// <summary>Returns active and inactive member counts.</summary>
    Task<DashboardMembersDto> GetMemberCountsAsync(CancellationToken cancellationToken);

    /// <summary>Returns unpaid obligation counts and debt for periods up to the supplied current month.</summary>
    Task<DashboardObligationsDto> GetObligationTotalsAsync(DateOnly currentMonth, CancellationToken cancellationToken);
}

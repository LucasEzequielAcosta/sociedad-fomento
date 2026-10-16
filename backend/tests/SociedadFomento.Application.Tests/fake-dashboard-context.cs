using SociedadFomento.Application.Dashboard;
using SociedadFomento.Application.Dashboard.Dto;

namespace SociedadFomento.Application.Tests;

internal sealed class FakeDashboardContext : IDashboardContext
{
    internal DashboardMembersDto Members { get; set; } = new(0, 0);
    internal DashboardObligationsDto Obligations { get; set; } = new(0, 0, 0m);

    public Task<DashboardMembersDto> GetMemberCountsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Members);

    public Task<DashboardObligationsDto> GetObligationTotalsAsync(DateOnly currentMonth, CancellationToken cancellationToken) =>
        Task.FromResult(Obligations);
}

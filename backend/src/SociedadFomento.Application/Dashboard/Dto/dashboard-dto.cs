using SociedadFomento.Application.Accounting.Dto;

namespace SociedadFomento.Application.Dashboard.Dto;

/// <summary>Represents the administrative dashboard summary.</summary>
public sealed record DashboardDto(
    DashboardMembersDto Members,
    DashboardObligationsDto Obligations,
    AccountingSummaryDto Economy);

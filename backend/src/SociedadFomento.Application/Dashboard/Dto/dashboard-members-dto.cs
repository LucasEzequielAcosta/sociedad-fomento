namespace SociedadFomento.Application.Dashboard.Dto;

/// <summary>Represents active and inactive member counts for the dashboard.</summary>
public sealed record DashboardMembersDto(int Active, int Inactive);

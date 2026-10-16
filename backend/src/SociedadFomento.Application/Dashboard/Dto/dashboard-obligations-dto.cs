namespace SociedadFomento.Application.Dashboard.Dto;

/// <summary>Represents unpaid fee-obligation counts and the total member debt.</summary>
public sealed record DashboardObligationsDto(int Pending, int Overdue, decimal TotalDebt);

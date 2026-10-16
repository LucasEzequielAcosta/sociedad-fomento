namespace SociedadFomento.Api.Contracts.Dashboard;

/// <summary>Represents the administrative dashboard summary returned by the API.</summary>
public sealed record DashboardResponse(
    DashboardMembersResponse Members,
    DashboardObligationsResponse Obligations,
    DashboardEconomyResponse Economy);

/// <summary>Represents active and inactive member counts.</summary>
public sealed record DashboardMembersResponse(int Active, int Inactive);

/// <summary>Represents unpaid obligation counts and total member debt.</summary>
public sealed record DashboardObligationsResponse(int Pending, int Overdue, decimal TotalDebt);

/// <summary>Represents income, expense and balance totals for the current month.</summary>
public sealed record DashboardEconomyResponse(DateOnly From, DateOnly To, decimal TotalIncome, decimal TotalExpense, decimal Balance);

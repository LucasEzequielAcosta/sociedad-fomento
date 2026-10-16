namespace SociedadFomento.Api.Contracts.Accounting;

/// <summary>Represents income, expense and balance totals for a date range.</summary>
public sealed record AccountingSummaryResponse(DateOnly From, DateOnly To, decimal TotalIncome, decimal TotalExpense, decimal Balance);

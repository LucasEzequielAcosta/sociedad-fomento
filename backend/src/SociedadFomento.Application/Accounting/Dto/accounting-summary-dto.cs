namespace SociedadFomento.Application.Accounting.Dto;

/// <summary>Represents active income, expense and balance totals for a date range.</summary>
public sealed record AccountingSummaryDto(
    DateOnly From,
    DateOnly To,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance);

namespace SociedadFomento.Application.Accounting.Models;

/// <summary>Represents the period selection for an accounting summary: a named period or an explicit date range.</summary>
public sealed record GetAccountingSummaryModel(AccountingSummaryPeriod? Period, DateOnly? From, DateOnly? To);

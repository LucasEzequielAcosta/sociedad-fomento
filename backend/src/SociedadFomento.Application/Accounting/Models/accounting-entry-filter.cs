using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Accounting.Models;

/// <summary>Represents optional filters used to list accounting entries.</summary>
public sealed record AccountingEntryFilter(DateOnly? From, DateOnly? To, AccountingEntryType? Type, string? Concept);

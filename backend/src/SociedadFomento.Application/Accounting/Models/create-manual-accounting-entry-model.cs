using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Accounting.Models;

/// <summary>Represents the input required to register a manual income or expense.</summary>
public sealed record CreateManualAccountingEntryModel(DateOnly EntryDate, AccountingEntryType Type, string Concept, decimal Amount);

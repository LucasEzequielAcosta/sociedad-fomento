using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Api.Contracts.Accounting;

/// <summary>Represents a request to register a manual income or expense.</summary>
public sealed record CreateManualAccountingEntryRequest(DateOnly EntryDate, AccountingEntryType Type, string Concept, decimal Amount);

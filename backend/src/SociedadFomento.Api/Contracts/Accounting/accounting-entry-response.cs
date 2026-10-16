using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Api.Contracts.Accounting;

/// <summary>Represents an accounting entry returned by the API.</summary>
public sealed record AccountingEntryResponse(
    long Id,
    DateOnly EntryDate,
    AccountingEntryType Type,
    AccountingEntrySource Source,
    string Concept,
    decimal Amount,
    AccountingEntryStatus Status,
    long? PaymentId,
    DateTime? CancelledAtUtc,
    DateTime CreatedAtUtc);

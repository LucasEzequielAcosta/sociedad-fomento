using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Accounting.Dto;

/// <summary>Represents an accounting entry in the income/expense ledger.</summary>
public sealed record AccountingEntryDto(
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

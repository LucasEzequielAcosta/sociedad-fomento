using SociedadFomento.Domain.Constants;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Domain.Entities;

/// <summary>Represents an immutable accounting income or expense that can be cancelled.</summary>
public sealed class AccountingEntry
{
    private AccountingEntry() { }

    private AccountingEntry(DateOnly entryDate, AccountingEntryType type, AccountingEntrySource source, string concept, decimal amount, DateTime createdAtUtc)
    {
        EntryDate = entryDate;
        Type = type;
        Source = source;
        Concept = EnsureConcept(concept);
        Amount = amount > 0 ? amount : throw new ArgumentOutOfRangeException(nameof(amount));
        Status = AccountingEntryStatus.Active;
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; private set; }
    public DateOnly EntryDate { get; private set; }
    public AccountingEntryType Type { get; private set; }
    public AccountingEntrySource Source { get; private set; }
    public string Concept { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public AccountingEntryStatus Status { get; private set; }
    public long? PaymentId { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public Payment? Payment { get; private set; }

    /// <summary>Creates the automatic income associated with a payment using the payment date.</summary>
    public static AccountingEntry CreateForPayment(Payment payment, string concept, DateTime createdAtUtc)
    {
        AccountingEntry entry = new(payment.PaymentDate, AccountingEntryType.Income, AccountingEntrySource.FeePayment, concept, payment.Amount, createdAtUtc)
        {
            Payment = payment
        };
        payment.SetAccountingEntry(entry);
        return entry;
    }

    /// <summary>Creates an immutable manual income or expense.</summary>
    public static AccountingEntry CreateManual(DateOnly entryDate, AccountingEntryType type, string concept, decimal amount, DateTime createdAtUtc)
    {
        return new AccountingEntry(entryDate, type, AccountingEntrySource.Manual, concept, amount, createdAtUtc);
    }

    /// <summary>Cancels the entry while retaining its history.</summary>
    public void Cancel(DateTime cancelledAtUtc)
    {
        if (Status == AccountingEntryStatus.Cancelled)
        {
            throw new InvalidOperationException("The accounting entry is already cancelled.");
        }

        Status = AccountingEntryStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
    }

    private static string EnsureConcept(string value)
    {
        string concept = value.Trim();
        return concept.Length is > 0 && concept.Length <= AccountingEntryFieldLengths.Concept
            ? concept
            : throw new ArgumentException($"The concept is required and cannot exceed {AccountingEntryFieldLengths.Concept} characters.", nameof(value));
    }
}

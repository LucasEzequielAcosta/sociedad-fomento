namespace SociedadFomento.Domain.Enums;

/// <summary>Represents how an accounting entry was created.</summary>
public enum AccountingEntrySource : byte
{
    FeePayment = 1,
    Manual = 2
}

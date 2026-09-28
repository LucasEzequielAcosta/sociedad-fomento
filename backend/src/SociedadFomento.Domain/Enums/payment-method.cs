namespace SociedadFomento.Domain.Enums;

/// <summary>Represents a supported payment method.</summary>
public enum PaymentMethod : byte
{
    Cash = 1,
    BankTransfer = 2,
    DirectDebit = 3
}

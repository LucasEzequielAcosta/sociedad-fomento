namespace SociedadFomento.Domain.Enums;

/// <summary>Represents the calculated status of a monthly fee obligation.</summary>
public enum FeeObligationStatus : byte
{
    Future = 1,
    Pending = 2,
    Paid = 3,
    Overdue = 4
}

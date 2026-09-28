namespace SociedadFomento.Domain.Entities;

/// <summary>Represents a monthly fee amount effective from a specific month.</summary>
public sealed class FeeRate
{
    private readonly List<FeeObligation> feeObligations = [];

    private FeeRate() { }

    /// <summary>Creates an immutable fee rate.</summary>
    public FeeRate(DateOnly effectiveMonth, decimal amount, DateTime createdAtUtc)
    {
        EffectiveMonth = NormalizeMonth(effectiveMonth);
        Amount = EnsurePositive(amount);
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; private set; }
    public DateOnly EffectiveMonth { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public IReadOnlyCollection<FeeObligation> FeeObligations => feeObligations;

    private static DateOnly NormalizeMonth(DateOnly value) => new(value.Year, value.Month, 1);

    private static decimal EnsurePositive(decimal value) => value > 0
        ? value
        : throw new ArgumentOutOfRangeException(nameof(value));
}

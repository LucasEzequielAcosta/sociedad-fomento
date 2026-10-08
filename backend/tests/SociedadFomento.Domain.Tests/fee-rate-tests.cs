using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Domain.Tests;

/// <summary>Tests fee-rate invariants.</summary>
public sealed class FeeRateTests
{
    /// <summary>Verifies that the effective month is normalized to the first day of the month.</summary>
    [Fact]
    public void Constructor_NormalizesEffectiveMonthToFirstDay()
    {
        FeeRate feeRate = new(new DateOnly(2026, 7, 15), 7000m, TestData.CreatedAtUtc);

        Assert.Equal(new DateOnly(2026, 7, 1), feeRate.EffectiveMonth);
    }

    /// <summary>Verifies that a positive amount is stored.</summary>
    [Fact]
    public void Constructor_AcceptsPositiveAmount()
    {
        FeeRate feeRate = new(new DateOnly(2026, 1, 1), 5000m, TestData.CreatedAtUtc);

        Assert.Equal(5000m, feeRate.Amount);
    }

    /// <summary>Verifies that a zero amount is rejected.</summary>
    [Fact]
    public void Constructor_RejectsZeroAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FeeRate(new DateOnly(2026, 1, 1), 0m, TestData.CreatedAtUtc));
    }

    /// <summary>Verifies that a negative amount is rejected.</summary>
    [Fact]
    public void Constructor_RejectsNegativeAmount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new FeeRate(new DateOnly(2026, 1, 1), -100m, TestData.CreatedAtUtc));
    }
}

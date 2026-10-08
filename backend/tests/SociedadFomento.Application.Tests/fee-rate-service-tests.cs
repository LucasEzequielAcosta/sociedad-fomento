using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.FeeRates;
using SociedadFomento.Application.FeeRates.Dto;
using SociedadFomento.Application.FeeRates.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests fee-rate application workflows.</summary>
public sealed class FeeRateServiceTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Verifies that creating a fee rate normalizes the month and returns the persisted entry.</summary>
    [Fact]
    public async Task Create_StoresNormalizedFeeRate()
    {
        (FeeRateService service, FakeFeeRateContext context) = CreateService();

        FeeRateDto result = await service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 7, 15), 7000m), CreatedAtUtc);

        Assert.Equal(new DateOnly(2026, 7, 1), result.EffectiveMonth);
        Assert.Equal(7000m, result.Amount);
        Assert.Single(context.FeeRates);
    }

    /// <summary>Verifies that a duplicate effective month is rejected.</summary>
    [Fact]
    public async Task Create_RejectsDuplicateMonth()
    {
        (FeeRateService service, FakeFeeRateContext context) = CreateService();
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 7, 1), 5000m, CreatedAtUtc));

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 7, 1), 7000m), CreatedAtUtc));

        Assert.Equal("FEE_RATE_MONTH_DUPLICATE", exception.Code);
    }

    /// <summary>Verifies that a zero amount is rejected.</summary>
    [Fact]
    public async Task Create_RejectsZeroAmount()
    {
        (FeeRateService service, _) = CreateService();

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 7, 1), 0m), CreatedAtUtc));

        Assert.Equal("INVALID_FEE_RATE_AMOUNT", exception.Code);
    }

    /// <summary>Verifies that the history is returned ordered by effective month.</summary>
    [Fact]
    public async Task GetAll_ReturnsOrderedHistory()
    {
        (FeeRateService service, FakeFeeRateContext context) = CreateService();
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 10, 1), 9000m, CreatedAtUtc));
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 1, 1), 5000m, CreatedAtUtc));
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 7, 1), 7000m, CreatedAtUtc));

        IReadOnlyList<FeeRateDto> result = await service.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), result[0].EffectiveMonth);
        Assert.Equal(new DateOnly(2026, 7, 1), result[1].EffectiveMonth);
        Assert.Equal(new DateOnly(2026, 10, 1), result[2].EffectiveMonth);
    }

    /// <summary>Verifies that the applicable rate is the most recent one not after the period.</summary>
    [Fact]
    public async Task GetApplicable_ReturnsMostRecentMatchingRate()
    {
        (FeeRateService service, FakeFeeRateContext context) = CreateService();
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 1, 1), 5000m, CreatedAtUtc));
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 7, 1), 7000m, CreatedAtUtc));

        FeeRateDto result = await service.GetApplicableAsync(new DateOnly(2026, 9, 1));

        Assert.Equal(7000m, result.Amount);
    }

    /// <summary>Verifies that missing rate for the period throws.</summary>
    [Fact]
    public async Task GetApplicable_ThrowsWhenNoRateExists()
    {
        (FeeRateService service, _) = CreateService();

        ResourceNotFoundException exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetApplicableAsync(new DateOnly(2026, 9, 1)));

        Assert.Equal("FEE_RATE_NOT_FOUND", exception.Code);
    }

    private static (FeeRateService Service, FakeFeeRateContext Context) CreateService()
    {
        FakeFeeRateContext context = new();
        return (new FeeRateService(context), context);
    }
}

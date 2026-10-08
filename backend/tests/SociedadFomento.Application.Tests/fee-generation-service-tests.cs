using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.FeeGeneration;
using SociedadFomento.Application.FeeGeneration.Dto;
using SociedadFomento.Application.FeeGeneration.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests monthly fee-obligation generation workflows.</summary>
public sealed class FeeGenerationServiceTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Verifies that one obligation is generated for each active member in the target month.</summary>
    [Fact]
    public async Task Generate_CreatesObligationForActiveMember()
    {
        (FeeGenerationService service, FakeFeeGenerationContext context) = CreateService();
        Member member = CreateMember("12345678", new DateOnly(2026, 9, 1));
        context.MembershipPeriods.Add(member.MembershipPeriods.Single());
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 9, 1), 5000m, CreatedAtUtc));

        FeeGenerationResult result = await service.GenerateAsync(new GenerateFeeObligationsModel(new DateOnly(2026, 10, 1)), CreatedAtUtc);

        Assert.Equal(new DateOnly(2026, 10, 1), result.Month);
        Assert.Equal(1, result.GeneratedCount);
        Assert.Single(context.Obligations);
        Assert.Equal(5000m, context.Obligations.Single().Amount);
    }

    /// <summary>Verifies that obligations are not generated for inactive members.</summary>
    [Fact]
    public async Task Generate_SkipsInactiveMember()
    {
        (FeeGenerationService service, FakeFeeGenerationContext context) = CreateService();
        Member member = CreateMember("12345678", new DateOnly(2026, 9, 1));
        member.Deactivate(new DateOnly(2026, 9, 15));
        context.MembershipPeriods.AddRange(member.MembershipPeriods);
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 9, 1), 5000m, CreatedAtUtc));

        FeeGenerationResult result = await service.GenerateAsync(new GenerateFeeObligationsModel(new DateOnly(2026, 10, 1)), CreatedAtUtc);

        Assert.Equal(0, result.GeneratedCount);
        Assert.Empty(context.Obligations);
    }

    /// <summary>Verifies that an existing obligation for the month prevents duplication.</summary>
    [Fact]
    public async Task Generate_DoesNotDuplicateExistingObligation()
    {
        (FeeGenerationService service, FakeFeeGenerationContext context) = CreateService();
        Member member = CreateMember("12345678", new DateOnly(2026, 9, 1));
        MembershipPeriod period = member.MembershipPeriods.Single();
        FeeRate rate = new(new DateOnly(2026, 9, 1), 5000m, CreatedAtUtc);
        context.MembershipPeriods.Add(period);
        context.FeeRates.Add(rate);
        context.Obligations.Add(new FeeObligation(member, period, rate, new DateOnly(2026, 10, 1), CreatedAtUtc));

        FeeGenerationResult result = await service.GenerateAsync(new GenerateFeeObligationsModel(new DateOnly(2026, 10, 1)), CreatedAtUtc);

        Assert.Equal(0, result.GeneratedCount);
        Assert.Single(context.Obligations);
    }

    /// <summary>Verifies that generation fails when no fee rate exists for the month.</summary>
    [Fact]
    public async Task Generate_WithoutFeeRateThrowsBusinessRuleException()
    {
        (FeeGenerationService service, FakeFeeGenerationContext context) = CreateService();
        Member member = CreateMember("12345678", new DateOnly(2026, 9, 1));
        context.MembershipPeriods.Add(member.MembershipPeriods.Single());

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.GenerateAsync(new GenerateFeeObligationsModel(new DateOnly(2026, 10, 1)), CreatedAtUtc));

        Assert.Equal("FEE_RATE_NOT_FOUND", exception.Code);
    }

    /// <summary>Verifies that the effective month is normalized to the first day.</summary>
    [Fact]
    public async Task Generate_NormalizesMonthToFirstDay()
    {
        (FeeGenerationService service, FakeFeeGenerationContext context) = CreateService();
        Member member = CreateMember("12345678", new DateOnly(2026, 9, 1));
        context.MembershipPeriods.Add(member.MembershipPeriods.Single());
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 9, 1), 5000m, CreatedAtUtc));

        FeeGenerationResult result = await service.GenerateAsync(new GenerateFeeObligationsModel(new DateOnly(2026, 10, 15)), CreatedAtUtc);

        Assert.Equal(new DateOnly(2026, 10, 1), result.Month);
    }

    private static (FeeGenerationService Service, FakeFeeGenerationContext Context) CreateService()
    {
        FakeFeeGenerationContext context = new();
        return (new FeeGenerationService(context), context);
    }

    private static Member CreateMember(string dni, DateOnly registrationDate) => new(
        dni, "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null, registrationDate);
}

using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests transactional member application workflows.</summary>
public sealed class MemberServiceTests
{
    private static readonly DateTime CurrentUtc = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Verifies that registration creates one active period and only the current obligation.</summary>
    [Fact]
    public async Task Create_CreatesCurrentPeriodAndObligation()
    {
        (MemberService service, FakeMemberContext context) = CreateService(withRate: true);

        await service.CreateAsync(CreateModel("12.345.678"));

        Member member = Assert.Single(context.Members);
        FeeObligation obligation = Assert.Single(context.Obligations);
        Assert.Equal("12345678", member.Dni);
        Assert.Equal(new DateOnly(2026, 10, 20), member.RegistrationDate);
        Assert.Equal(new DateOnly(2026, 10, 1), obligation.Period);
        Assert.Equal(MemberStatus.Active, member.Status);
    }

    /// <summary>Verifies that a normalized duplicate DNI is rejected.</summary>
    [Fact]
    public async Task Create_RejectsNormalizedDuplicateDni()
    {
        (MemberService service, FakeMemberContext context) = CreateService(withRate: true);
        context.Members.Add(CreateExistingMember("12345678"));

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(CreateModel("12.345.678")));

        Assert.Equal("DNI_DUPLICATE", exception.Code);
        Assert.Empty(context.Obligations);
    }

    /// <summary>Verifies complete rollback behavior when registration has no applicable fee rate.</summary>
    [Fact]
    public async Task Create_WithoutFeeRateDoesNotCreateMemberOrObligation()
    {
        (MemberService service, FakeMemberContext context) = CreateService(withRate: false);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(CreateModel("12345678")));

        Assert.Equal("FEE_RATE_NOT_FOUND", exception.Code);
        Assert.Empty(context.Members);
        Assert.Empty(context.Obligations);
    }

    /// <summary>Verifies that deactivation uses the deterministic current date and preserves obligations.</summary>
    [Fact]
    public async Task Deactivate_ClosesPeriodWithoutChangingObligation()
    {
        (MemberService service, FakeMemberContext context) = CreateService(withRate: true);
        Member member = CreateExistingMember("12345678");
        FeeObligation obligation = AddExistingObligation(context, member, new DateOnly(2026, 10, 1));
        context.Members.Add(member);

        await service.DeactivateAsync(1);

        Assert.Equal(MemberStatus.Inactive, member.Status);
        Assert.Equal(new DateOnly(2026, 10, 20), member.MembershipPeriods.Single().EndDate);
        Assert.Same(obligation, Assert.Single(context.Obligations));
    }

    /// <summary>Verifies that reactivation preserves an existing obligation snapshot and original registration date.</summary>
    [Fact]
    public async Task Reactivate_PreservesExistingObligationExactly()
    {
        (MemberService service, FakeMemberContext context) = CreateService(withRate: true);
        Member member = CreateInactiveMember();
        FeeObligation obligation = AddExistingObligation(context, member, new DateOnly(2026, 10, 1));
        MembershipPeriod originalPeriod = obligation.MembershipPeriod;
        FeeRate originalRate = obligation.FeeRate;
        decimal originalAmount = obligation.Amount;
        DateOnly registrationDate = member.RegistrationDate;
        context.Members.Add(member);

        ReactivateMemberResult result = await service.ReactivateAsync(1);

        Assert.False(result.ObligationCreated);
        Assert.Same(originalPeriod, obligation.MembershipPeriod);
        Assert.Same(originalRate, obligation.FeeRate);
        Assert.Equal(originalAmount, obligation.Amount);
        Assert.Equal(registrationDate, member.RegistrationDate);
    }

    /// <summary>Verifies that reactivation creates the current obligation on the new period when absent.</summary>
    [Fact]
    public async Task Reactivate_CreatesObligationForNewPeriod()
    {
        (MemberService service, FakeMemberContext context) = CreateService(withRate: true);
        Member member = CreateInactiveMember();
        context.Members.Add(member);

        ReactivateMemberResult result = await service.ReactivateAsync(1);

        FeeObligation obligation = Assert.Single(context.Obligations);
        MembershipPeriod currentPeriod = member.MembershipPeriods.Single(period => !period.EndDate.HasValue);
        Assert.True(result.ObligationCreated);
        Assert.Same(currentPeriod, obligation.MembershipPeriod);
        Assert.Equal(new DateOnly(2026, 10, 1), obligation.Period);
    }

    /// <summary>Verifies that missing fee rate leaves an inactive member unchanged during reactivation.</summary>
    [Fact]
    public async Task Reactivate_WithoutFeeRateDoesNotMutateMember()
    {
        (MemberService service, FakeMemberContext context) = CreateService(withRate: false);
        Member member = CreateInactiveMember();
        int periodCount = member.MembershipPeriods.Count;
        context.Members.Add(member);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ReactivateAsync(1));

        Assert.Equal(MemberStatus.Inactive, member.Status);
        Assert.Equal(periodCount, member.MembershipPeriods.Count);
        Assert.Empty(context.Obligations);
    }

    private static (MemberService Service, FakeMemberContext Context) CreateService(bool withRate)
    {
        FakeMemberContext context = new();
        if (withRate)
        {
            context.FeeRates.Add(new FeeRate(new DateOnly(2026, 10, 1), 5000m, CurrentUtc));
        }
        return (new MemberService(context, new FixedClock(CurrentUtc)), context);
    }

    private static CreateMemberModel CreateModel(string dni) => new(dni,
        new MemberPersonalData("Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", "ana@example.com"));

    private static Member CreateExistingMember(string dni) => new(
        dni, "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null, new DateOnly(2026, 9, 1));

    private static Member CreateInactiveMember()
    {
        Member member = CreateExistingMember("12345678");
        member.Deactivate(new DateOnly(2026, 10, 19));
        return member;
    }

    private static FeeObligation AddExistingObligation(FakeMemberContext context, Member member, DateOnly period)
    {
        MembershipPeriod membershipPeriod = member.MembershipPeriods.First();
        FeeRate rate = context.FeeRates.Single();
        FeeObligation obligation = new(member, membershipPeriod, rate, period, CurrentUtc);
        context.Obligations.Add(obligation);
        return obligation;
    }
}

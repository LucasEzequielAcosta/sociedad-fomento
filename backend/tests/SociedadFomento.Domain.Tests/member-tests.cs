using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Domain.Tests;

/// <summary>Tests member lifecycle behavior.</summary>
public sealed class MemberTests
{
    /// <summary>Verifies canonical DNI normalization.</summary>
    [Fact]
    public void Constructor_NormalizesDni()
    {
        Member member = TestData.CreateMember();

        Assert.Equal("12345678", member.Dni);
    }

    /// <summary>Verifies that personal updates do not change historical identity fields.</summary>
    [Fact]
    public void UpdatePersonalData_PreservesDniAndRegistrationDate()
    {
        Member member = TestData.CreateMember();
        string dni = member.Dni;
        DateOnly registrationDate = member.RegistrationDate;

        member.UpdatePersonalData("María", "Gómez", "Calle 2", new DateOnly(1991, 2, 2), "123", "maria@example.com");

        Assert.Equal(dni, member.Dni);
        Assert.Equal(registrationDate, member.RegistrationDate);
        Assert.Equal("María", member.FirstName);
    }

    /// <summary>Verifies that deactivation closes the current period without altering an existing obligation.</summary>
    [Fact]
    public void Deactivate_PreservesExistingObligation()
    {
        Member member = TestData.CreateMember();
        FeeObligation obligation = TestData.CreateObligation(member, new DateOnly(2026, 9, 1));
        DateOnly currentDate = new(2026, 9, 20);

        member.Deactivate(currentDate);

        Assert.Equal(MemberStatus.Inactive, member.Status);
        Assert.Equal(currentDate, member.MembershipPeriods.Single().EndDate);
        Assert.Equal(FeeObligationStatus.Pending, obligation.GetStatus(currentDate));
    }

    /// <summary>Verifies that an obligation rejects a membership period owned by another member.</summary>
    [Fact]
    public void FeeObligation_RejectsMembershipPeriodFromAnotherMember()
    {
        Member member = TestData.CreateMember();
        Member otherMember = TestData.CreateMember(new DateOnly(2026, 10, 1));
        MembershipPeriod otherPeriod = otherMember.MembershipPeriods.Single();
        FeeRate rate = new(new DateOnly(2026, 10, 1), 5000m, TestData.CreatedAtUtc);

        Assert.Throws<ArgumentException>(() =>
            new FeeObligation(member, otherPeriod, rate, new DateOnly(2026, 10, 1), TestData.CreatedAtUtc));
    }

    /// <summary>Verifies that reactivation cannot overlap the deactivation date.</summary>
    [Fact]
    public void Reactivate_RejectsSameDayAsDeactivation()
    {
        Member member = TestData.CreateMember();
        DateOnly currentDate = new(2026, 9, 15);
        member.Deactivate(currentDate);

        Assert.Throws<InvalidOperationException>(() => member.Reactivate(currentDate));
    }

    /// <summary>Verifies that reactivation opens a distinct activity period in the same month.</summary>
    [Fact]
    public void Reactivate_OpensNewMembershipPeriod()
    {
        Member member = TestData.CreateMember();
        member.Deactivate(new DateOnly(2026, 9, 15));

        MembershipPeriod period = member.Reactivate(new DateOnly(2026, 9, 20));

        Assert.Equal(MemberStatus.Active, member.Status);
        Assert.Equal(new DateOnly(2026, 9, 20), period.StartDate);
        Assert.Equal(2, member.MembershipPeriods.Count);
    }
}

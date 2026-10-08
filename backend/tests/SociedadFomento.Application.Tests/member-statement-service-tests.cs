using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.MemberStatements;
using SociedadFomento.Application.MemberStatements.Dto;
using SociedadFomento.Application.MemberStatements.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests member account-statement and debt-calculation behavior.</summary>
public sealed class MemberStatementServiceTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Verifies that the statement contains obligations, payments and correct debt.</summary>
    [Fact]
    public async Task Get_ReturnsStatementWithDebtAndPayments()
    {
        (MemberStatementService service, FakeMemberStatementContext context, Member member, List<FeeObligation> obligations) = CreateScenario();
        Payment payment = new(member, new DateOnly(2026, 10, 15), 14000m, PaymentMethod.Cash, obligations, CreatedAtUtc);
        context.Payments.Add(payment);
        SetPrivateProperty(payment, nameof(Payment.Id), 1);
        SetPrivateProperty(payment, nameof(Payment.MemberId), member.Id);

        MemberStatementDto result = await service.GetAsync(new GetMemberStatementModel(member.Id, new DateOnly(2026, 10, 20)));

        Assert.Equal(member.Id, result.MemberId);
        Assert.Equal(2, result.Obligations.Count);
        Assert.Single(result.Payments);
        Assert.Equal(14000m, result.TotalPaid);
        Assert.Equal(0m, result.TotalDebt);
        Assert.All(result.Obligations, obligation => Assert.Equal(FeeObligationStatus.Paid, obligation.Status));
    }

    /// <summary>Verifies that future obligations are not included in debt.</summary>
    [Fact]
    public async Task Get_DebtExcludesFutureObligations()
    {
        (MemberStatementService service, _, Member member, List<FeeObligation> obligations) = CreateScenario();

        MemberStatementDto result = await service.GetAsync(new GetMemberStatementModel(member.Id, new DateOnly(2026, 9, 15)));

        Assert.Equal(FeeObligationStatus.Overdue, result.Obligations.Single(o => o.Period == new DateOnly(2026, 8, 1)).Status);
        Assert.Equal(FeeObligationStatus.Pending, result.Obligations.Single(o => o.Period == new DateOnly(2026, 9, 1)).Status);
        Assert.Equal(14000m, result.TotalDebt);
    }

    /// <summary>Verifies that cancelled payments do not count toward total paid.</summary>
    [Fact]
    public async Task Get_CancelledPaymentsAreNotIncludedInTotalPaid()
    {
        (MemberStatementService service, FakeMemberStatementContext context, Member member, List<FeeObligation> obligations) = CreateScenario();
        Payment payment = new(member, new DateOnly(2026, 10, 15), 14000m, PaymentMethod.Cash, obligations, CreatedAtUtc);
        payment.Cancel(CreatedAtUtc.AddDays(1));
        context.Payments.Add(payment);
        SetPrivateProperty(payment, nameof(Payment.Id), 1);
        SetPrivateProperty(payment, nameof(Payment.MemberId), member.Id);

        MemberStatementDto result = await service.GetAsync(new GetMemberStatementModel(member.Id, new DateOnly(2026, 10, 20)));

        Assert.Equal(0m, result.TotalPaid);
        Assert.Equal(14000m, result.TotalDebt);
    }

    /// <summary>Verifies that a missing member is rejected.</summary>
    [Fact]
    public async Task Get_MissingMemberThrowsResourceNotFound()
    {
        (MemberStatementService service, _, _, _) = CreateScenario();

        ResourceNotFoundException exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.GetAsync(new GetMemberStatementModel(999, new DateOnly(2026, 10, 20))));

        Assert.Equal("MEMBER_NOT_FOUND", exception.Code);
    }

    private static (MemberStatementService Service, FakeMemberStatementContext Context, Member Member, List<FeeObligation> Obligations) CreateScenario()
    {
        FakeMemberStatementContext context = new();
        Member member = CreateMember("12345678", new DateOnly(2026, 8, 1));
        SetPrivateProperty(member, nameof(Member.Id), 1L);
        context.Members.Add(member);
        FeeObligation august = CreateObligation(member, new DateOnly(2026, 8, 1), 7000m, 1);
        FeeObligation september = CreateObligation(member, new DateOnly(2026, 9, 1), 7000m, 2);
        context.Obligations.Add(august);
        context.Obligations.Add(september);
        return (new MemberStatementService(context), context, member, [august, september]);
    }

    private static Member CreateMember(string dni, DateOnly registrationDate) => new(
        dni, "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null, registrationDate);

    private static FeeObligation CreateObligation(Member member, DateOnly period, decimal amount, long id)
    {
        FeeRate rate = new(period, amount, CreatedAtUtc);
        FeeObligation obligation = new(member, member.MembershipPeriods.Single(), rate, period, CreatedAtUtc);
        SetPrivateProperty(obligation, nameof(FeeObligation.Id), id);
        SetPrivateProperty(obligation, nameof(FeeObligation.MemberId), member.Id);
        return obligation;
    }

    private static void SetPrivateProperty(object entity, string propertyName, object value)
    {
        System.Reflection.PropertyInfo property = entity.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(nonPublic: true)!.Invoke(entity, [value]);
    }
}

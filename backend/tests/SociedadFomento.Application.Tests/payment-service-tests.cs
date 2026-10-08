using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.Payments;
using SociedadFomento.Application.Payments.Dto;
using SociedadFomento.Application.Payments.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using System.Reflection;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests payment registration workflows.</summary>
public sealed class PaymentServiceTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
    private static long _nextId = 1;

    /// <summary>Verifies that a valid payment is registered and allocated.</summary>
    [Fact]
    public async Task Register_CreatesPaymentAndAllocations()
    {
        (PaymentService service, FakePaymentContext context, Member member, List<FeeObligation> obligations) = CreateScenario();

        PaymentDto result = await service.RegisterAsync(new CreatePaymentModel(
            member.Id, new DateOnly(2026, 10, 8), 10000m, PaymentMethod.Cash, obligations.Select(item => item.Id).ToList()), CreatedAtUtc);

        Assert.Single(context.Payments);
        Assert.Equal(member.Id, result.MemberId);
        Assert.Equal(10000m, result.Amount);
        Assert.Equal(2, result.Allocations.Count);
        Assert.Single(context.AccountingEntries);
    }

    /// <summary>Verifies that an amount mismatch is rejected.</summary>
    [Fact]
    public async Task Register_RejectsAmountMismatch()
    {
        (PaymentService service, _, Member member, List<FeeObligation> obligations) = CreateScenario();

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.RegisterAsync(new CreatePaymentModel(
                member.Id, new DateOnly(2026, 10, 8), 9999m, PaymentMethod.Cash, obligations.Select(item => item.Id).ToList()), CreatedAtUtc));

        Assert.Equal("INVALID_PAYMENT", exception.Code);
    }

    /// <summary>Verifies that obligations from another member are rejected.</summary>
    [Fact]
    public async Task Register_RejectsObligationsFromAnotherMember()
    {
        (PaymentService service, FakePaymentContext context, Member member, _) = CreateScenario();
        Member otherMember = CreateMember("87654321", new DateOnly(2026, 9, 1));
        SetPrivateProperty(otherMember, nameof(Member.Id), NextId());
        context.Members.Add(otherMember);
        FeeObligation otherObligation = CreateObligation(otherMember, new DateOnly(2026, 10, 1), 5000m, NextId());
        context.Obligations.Add(otherObligation);

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.RegisterAsync(new CreatePaymentModel(
                member.Id, new DateOnly(2026, 10, 8), 5000m, PaymentMethod.Cash, [otherObligation.Id]), CreatedAtUtc));

        Assert.Equal("INVALID_OBLIGATIONS", exception.Code);
    }

    /// <summary>Verifies that an already paid obligation is rejected.</summary>
    [Fact]
    public async Task Register_RejectsAlreadyPaidObligation()
    {
        (PaymentService service, FakePaymentContext context, Member member, List<FeeObligation> obligations) = CreateScenario();
        Payment previous = new(member, new DateOnly(2026, 10, 1), 5000m, PaymentMethod.Cash, [obligations[0]], CreatedAtUtc);
        context.Payments.Add(previous);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.RegisterAsync(new CreatePaymentModel(
                member.Id, new DateOnly(2026, 10, 8), 10000m, PaymentMethod.Cash, obligations.Select(item => item.Id).ToList()), CreatedAtUtc));

        Assert.Equal("OBLIGATION_ALREADY_PAID", exception.Code);
    }

    /// <summary>Verifies that duplicate obligation ids are rejected.</summary>
    [Fact]
    public async Task Register_RejectsDuplicateObligationIds()
    {
        (PaymentService service, _, Member member, List<FeeObligation> obligations) = CreateScenario();

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.RegisterAsync(new CreatePaymentModel(
                member.Id, new DateOnly(2026, 10, 8), 10000m, PaymentMethod.Cash, [obligations[0].Id, obligations[0].Id, obligations[1].Id]), CreatedAtUtc));

        Assert.Equal("DUPLICATE_OBLIGATIONS", exception.Code);
    }

    /// <summary>Verifies that no obligations is rejected.</summary>
    [Fact]
    public async Task Register_RejectsEmptyObligations()
    {
        (PaymentService service, _, Member member, _) = CreateScenario();

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.RegisterAsync(new CreatePaymentModel(
                member.Id, new DateOnly(2026, 10, 8), 1000m, PaymentMethod.Cash, []), CreatedAtUtc));

        Assert.Equal("MISSING_OBLIGATIONS", exception.Code);
    }

    /// <summary>Verifies that a missing member is rejected.</summary>
    [Fact]
    public async Task Register_RejectsMissingMember()
    {
        (PaymentService service, _, _, _) = CreateScenario();

        ResourceNotFoundException exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.RegisterAsync(new CreatePaymentModel(
                999, new DateOnly(2026, 10, 8), 1000m, PaymentMethod.Cash, [1]), CreatedAtUtc));

        Assert.Equal("MEMBER_NOT_FOUND", exception.Code);
    }

    /// <summary>Verifies that an invalid payment method is rejected.</summary>
    [Fact]
    public async Task Register_RejectsInvalidPaymentMethod()
    {
        (PaymentService service, _, Member member, List<FeeObligation> obligations) = CreateScenario();

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.RegisterAsync(new CreatePaymentModel(
                member.Id, new DateOnly(2026, 10, 8), 10000m, (PaymentMethod)99, obligations.Select(item => item.Id).ToList()), CreatedAtUtc));

        Assert.Equal("INVALID_PAYMENT_METHOD", exception.Code);
    }

    private static (PaymentService Service, FakePaymentContext Context, Member Member, List<FeeObligation> Obligations) CreateScenario()
    {
        FakePaymentContext context = new();
        Member member = CreateMember("12345678", new DateOnly(2026, 9, 1));
        SetPrivateProperty(member, nameof(Member.Id), NextId());
        context.Members.Add(member);
        FeeObligation first = CreateObligation(member, new DateOnly(2026, 9, 1), 5000m, NextId());
        FeeObligation second = CreateObligation(member, new DateOnly(2026, 10, 1), 5000m, NextId());
        context.Obligations.Add(first);
        context.Obligations.Add(second);
        return (new PaymentService(context), context, member, [first, second]);
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
        PropertyInfo property = entity.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(nonPublic: true)!.Invoke(entity, [value]);
    }

    private static long NextId() => _nextId++;
}

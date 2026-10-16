using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.Payments;
using SociedadFomento.Application.Payments.Dto;
using SociedadFomento.Application.Payments.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests payment listing filters and validation.</summary>
public sealed class PaymentServiceFilterTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Verifies that an incomplete or inverted date range is rejected.</summary>
    [Fact]
    public async Task GetPayments_RejectsInvalidRange()
    {
        PaymentService service = new(new FakePaymentContext());

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.GetPaymentsAsync(new PaymentFilter(null, new DateOnly(2026, 10, 1), null, null)));
        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.GetPaymentsAsync(new PaymentFilter(null, new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 1), null)));
    }

    /// <summary>Verifies that an undefined payment method is rejected.</summary>
    [Fact]
    public async Task GetPayments_RejectsInvalidMethod()
    {
        PaymentService service = new(new FakePaymentContext());

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.GetPaymentsAsync(new PaymentFilter(null, null, null, (PaymentMethod)99)));

        Assert.Equal("INVALID_PAYMENT_METHOD", exception.Code);
    }

    /// <summary>Verifies that payments are returned with member data and allocated periods.</summary>
    [Fact]
    public async Task GetPayments_ReturnsFilteredPayments()
    {
        FakePaymentContext context = new();
        Member member = new("12345678", "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null, new DateOnly(2026, 9, 1));
        context.Members.Add(member);
        FeeRate rate = new(new DateOnly(2026, 9, 1), 5000m, CreatedAtUtc);
        FeeObligation obligation = new(member, member.MembershipPeriods.Single(), rate, new DateOnly(2026, 9, 1), CreatedAtUtc);
        Payment payment = new(member, new DateOnly(2026, 9, 25), 5000m, PaymentMethod.Cash, [obligation], CreatedAtUtc);
        context.Payments.Add(payment);
        PaymentService service = new(context);

        IReadOnlyList<PaymentListItemDto> result = await service.GetPaymentsAsync(new PaymentFilter(null, null, null, null));

        PaymentListItemDto item = Assert.Single(result);
        Assert.Equal("12345678", item.MemberDni);
        Assert.Equal([new DateOnly(2026, 9, 1)], item.Periods);

        IReadOnlyList<PaymentListItemDto> transferOnly = await service.GetPaymentsAsync(new PaymentFilter(null, null, null, PaymentMethod.BankTransfer));
        Assert.Empty(transferOnly);
    }
}

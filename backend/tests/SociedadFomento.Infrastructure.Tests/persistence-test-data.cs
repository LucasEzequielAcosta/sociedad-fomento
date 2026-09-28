using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

internal static class PersistenceTestData
{
    internal static readonly DateTime CreatedAtUtc = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    internal static async Task<FeeObligation> AddObligationAsync(
        SociedadFomentoDbContext context, string dni, DateOnly period, decimal amount = 5000m)
    {
        Member member = CreateMember(dni);
        MembershipPeriod membershipPeriod = member.MembershipPeriods.Single();
        FeeRate rate = new(period, amount, CreatedAtUtc);
        FeeObligation obligation = new(member, membershipPeriod, rate, period, CreatedAtUtc);
        context.FeeObligations.Add(obligation);
        await context.SaveChangesAsync();
        return obligation;
    }

    internal static async Task<Payment> AddPaymentAsync(
        SociedadFomentoDbContext context, string dni, DateOnly period, DateOnly paymentDate)
    {
        FeeObligation obligation = await AddObligationAsync(context, dni, period);
        Payment payment = new(obligation.Member, paymentDate, obligation.Amount, PaymentMethod.Cash, [obligation], CreatedAtUtc);
        AccountingEntry.CreateForPayment(payment, "Pago de cuota", CreatedAtUtc);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        return payment;
    }

    private static Member CreateMember(string dni) => new(
        dni, "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "+54 223 555-0000", null, new DateOnly(2026, 9, 1));
}

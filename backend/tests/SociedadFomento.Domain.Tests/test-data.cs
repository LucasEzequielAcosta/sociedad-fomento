using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Domain.Tests;

internal static class TestData
{
    internal static readonly DateTime CreatedAtUtc = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    internal static Member CreateMember(DateOnly? registrationDate = null) => new(
        "12.345.678",
        "Ana",
        "Pérez",
        "Calle 1",
        new DateOnly(1990, 1, 1),
        "+54 223 555-0000",
        "ana@example.com",
        registrationDate ?? new DateOnly(2026, 9, 10));

    internal static FeeObligation CreateObligation(Member member, DateOnly period, decimal amount = 5000m)
    {
        MembershipPeriod membershipPeriod = member.MembershipPeriods.Single(period => !period.EndDate.HasValue);
        FeeRate rate = new(period, amount, CreatedAtUtc);
        return new FeeObligation(member, membershipPeriod, rate, period, CreatedAtUtc);
    }
}

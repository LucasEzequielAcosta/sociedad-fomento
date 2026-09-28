using SociedadFomento.Application.Abstractions;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests conversion from UTC instants to the fixed business calendar.</summary>
public sealed class BusinessTimeZoneTests
{
    /// <summary>Verifies that Argentina remains in the previous month after UTC midnight.</summary>
    [Fact]
    public void GetDate_UsesBuenosAiresCalendarNearMonthBoundary()
    {
        DateTime utcNow = new(2026, 10, 1, 1, 30, 0, DateTimeKind.Utc);

        DateOnly businessDate = BusinessTimeZone.GetDate(utcNow);

        Assert.Equal(new DateOnly(2026, 9, 30), businessDate);
    }
}

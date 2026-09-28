using SociedadFomento.Application.Abstractions;

namespace SociedadFomento.Application.Tests;

internal sealed class FixedClock : IClock
{
    internal FixedClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; }
    public DateOnly Today => BusinessTimeZone.GetDate(UtcNow);
}

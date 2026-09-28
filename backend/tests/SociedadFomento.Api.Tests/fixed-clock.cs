using SociedadFomento.Application.Abstractions;

namespace SociedadFomento.Api.Tests;

internal sealed class FixedClock : IClock
{
    internal FixedClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTime UtcNow { get; private set; }
    public DateOnly Today => BusinessTimeZone.GetDate(UtcNow);

    internal void SetUtcNow(DateTime utcNow) => UtcNow = utcNow;
}

using SociedadFomento.Application.Abstractions;

namespace SociedadFomento.Infrastructure.Tests;

internal sealed class FixedClock : IClock
{
    private DateTime utcNow;

    internal FixedClock(DateTime utcNow)
    {
        this.utcNow = utcNow;
    }

    public DateTime UtcNow => utcNow;

    public DateOnly Today => BusinessTimeZone.GetDate(utcNow);

    internal void SetUtcNow(DateTime value) => utcNow = value;
}

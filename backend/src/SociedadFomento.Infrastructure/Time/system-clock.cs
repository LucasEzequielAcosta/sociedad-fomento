using SociedadFomento.Application.Abstractions;

namespace SociedadFomento.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateOnly Today => BusinessTimeZone.GetDate(UtcNow);
}

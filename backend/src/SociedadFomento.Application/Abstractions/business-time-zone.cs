namespace SociedadFomento.Application.Abstractions;

/// <summary>Defines the fixed business time zone used to calculate calendar dates.</summary>
public static class BusinessTimeZone
{
    public const string Id = "America/Argentina/Buenos_Aires";

    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById(Id);

    /// <summary>Returns the business-local date corresponding to a UTC instant.</summary>
    public static DateOnly GetDate(DateTime utcNow)
    {
        DateTime normalizedUtc = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, TimeZone);
        return DateOnly.FromDateTime(localTime);
    }
}

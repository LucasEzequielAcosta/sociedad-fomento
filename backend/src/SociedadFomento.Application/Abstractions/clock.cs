namespace SociedadFomento.Application.Abstractions;

/// <summary>Provides the current date and time for application operations.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}

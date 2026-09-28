namespace SociedadFomento.Application.Common.Exceptions;

/// <summary>Represents a requested resource that does not exist.</summary>
public sealed class ResourceNotFoundException : Exception
{
    /// <summary>Creates a resource not found exception.</summary>
    public ResourceNotFoundException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

namespace SociedadFomento.Application.Common.Exceptions;

/// <summary>Represents invalid application input.</summary>
public sealed class RequestValidationException : Exception
{
    /// <summary>Creates a request validation exception.</summary>
    public RequestValidationException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

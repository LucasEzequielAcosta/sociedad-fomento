namespace SociedadFomento.Application.Common.Exceptions;

/// <summary>Represents a stable business rule conflict.</summary>
public sealed class BusinessRuleException : Exception
{
    /// <summary>Creates a business rule exception.</summary>
    public BusinessRuleException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

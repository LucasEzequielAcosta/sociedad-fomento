using SociedadFomento.Domain.Constants;

namespace SociedadFomento.Domain.Entities;

/// <summary>Normalizes and validates member DNI values.</summary>
public static class DniNormalizer
{
    /// <summary>Returns the canonical DNI without punctuation or spaces.</summary>
    public static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        string normalized = value.Trim().Replace(".", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
        return normalized.Length is > 0 && normalized.Length <= MemberFieldLengths.Dni
            ? normalized
            : throw new ArgumentException("The DNI is required and exceeds its allowed length.", nameof(value));
    }
}

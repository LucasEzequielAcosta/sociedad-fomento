using SociedadFomento.Domain.Constants;

namespace SociedadFomento.Domain.Entities;

/// <summary>Normalizes administrator email addresses consistently.</summary>
public static class EmailNormalizer
{
    /// <summary>Returns a canonical email used for lookup and uniqueness.</summary>
    public static string Normalize(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        string normalized = email.Trim().ToUpperInvariant();
        return normalized.Length <= AdminUserFieldLengths.NormalizedEmail
            ? normalized
            : throw new ArgumentException("Email exceeds its allowed length.", nameof(email));
    }
}

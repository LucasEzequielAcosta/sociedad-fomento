using SociedadFomento.Domain.Constants;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Domain.Entities;

/// <summary>Represents the single administrative identity used by the MVP.</summary>
public sealed class AdminUser
{
    private AdminUser() { }

    private AdminUser(string email, DateTime createdAtUtc)
    {
        Email = EnsureRequired(email.Trim(), AdminUserFieldLengths.Email, nameof(email));
        NormalizedEmail = EmailNormalizer.Normalize(email);
        CreatedAtUtc = createdAtUtc.Kind == DateTimeKind.Utc
            ? createdAtUtc
            : throw new ArgumentException("Created timestamp must be UTC.", nameof(createdAtUtc));
        Role = AdminRole.Admin;
        IsActive = true;
    }

    /// <summary>Creates an active administrator from an already generated password hash.</summary>
    public AdminUser(string email, string passwordHash, DateTime createdAtUtc) : this(email, createdAtUtc)
    {
        PasswordHash = EnsureRequired(passwordHash, AdminUserFieldLengths.PasswordHash, nameof(passwordHash));
    }

    /// <summary>Creates an administrator and lets the password service hash against that user instance.</summary>
    public static AdminUser Create(string email, DateTime createdAtUtc, Func<AdminUser, string> passwordHashFactory)
    {
        AdminUser user = new(email, createdAtUtc);
        user.PasswordHash = EnsureRequired(passwordHashFactory(user), AdminUserFieldLengths.PasswordHash, nameof(passwordHashFactory));
        return user;
    }

    public long Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public AdminRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Replaces only an outdated password hash after a successful verification.</summary>
    public void ReplacePasswordHashForRehash(string passwordHash)
    {
        PasswordHash = EnsureRequired(passwordHash, AdminUserFieldLengths.PasswordHash, nameof(passwordHash));
    }

    private static string EnsureRequired(string value, int maximumLength, string parameterName) =>
        value.Length is > 0 && value.Length <= maximumLength
            ? value
            : throw new ArgumentException("The value is required and exceeds its allowed length.", parameterName);
}

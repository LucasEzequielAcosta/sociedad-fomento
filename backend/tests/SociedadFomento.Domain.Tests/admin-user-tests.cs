using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Domain.Tests;

/// <summary>Tests administrator identity invariants.</summary>
public sealed class AdminUserTests
{
    /// <summary>Verifies normalized email, role and controlled hash replacement.</summary>
    [Fact]
    public void ConstructorAndRehash_PreserveIdentity()
    {
        DateTime createdAtUtc = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        AdminUser user = new(" Admin@Example.com ", "initial-hash", createdAtUtc);

        user.ReplacePasswordHashForRehash("replacement-hash");

        Assert.Equal("Admin@Example.com", user.Email);
        Assert.Equal("ADMIN@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal(AdminRole.Admin, user.Role);
        Assert.True(user.IsActive);
        Assert.Equal("replacement-hash", user.PasswordHash);
        Assert.Equal(createdAtUtc, user.CreatedAtUtc);
    }
}

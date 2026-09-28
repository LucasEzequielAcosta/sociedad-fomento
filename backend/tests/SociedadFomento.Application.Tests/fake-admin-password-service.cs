using SociedadFomento.Application.Authentication;
using SociedadFomento.Application.Authentication.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

internal sealed class FakeAdminPasswordService : IAdminPasswordService
{
    internal PasswordVerificationStatus Verification { get; set; } = PasswordVerificationStatus.Success;
    internal string GeneratedHash { get; set; } = "generated-hash";

    public string HashPassword(AdminUser user, string password) => GeneratedHash;
    public PasswordVerificationStatus VerifyHashedPassword(AdminUser user, string password) => Verification;
}

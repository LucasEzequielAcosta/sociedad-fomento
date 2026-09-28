using Microsoft.AspNetCore.Identity;
using SociedadFomento.Application.Authentication;
using SociedadFomento.Application.Authentication.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Infrastructure.Authentication;

internal sealed class AdminPasswordService : IAdminPasswordService
{
    private readonly IPasswordHasher<AdminUser> passwordHasher;

    public AdminPasswordService(IPasswordHasher<AdminUser> passwordHasher)
    {
        this.passwordHasher = passwordHasher;
    }

    public string HashPassword(AdminUser user, string password) => passwordHasher.HashPassword(user, password);

    public PasswordVerificationStatus VerifyHashedPassword(AdminUser user, string password) =>
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerificationStatus.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationStatus.SuccessRehashNeeded,
            _ => PasswordVerificationStatus.Failed
        };
}

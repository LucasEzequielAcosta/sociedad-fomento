using SociedadFomento.Application.Authentication.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Authentication;

/// <summary>Defines secure administrator password hashing operations.</summary>
public interface IAdminPasswordService
{
    string HashPassword(AdminUser user, string password);
    PasswordVerificationStatus VerifyHashedPassword(AdminUser user, string password);
}

using SociedadFomento.Application.Authentication.Dto;
using SociedadFomento.Application.Authentication.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Authentication;

/// <summary>Verifies administrator credentials without exposing failure causes.</summary>
public sealed class AdminAuthenticationService
{
    private readonly IAdminAuthenticationContext context;
    private readonly IAdminPasswordService passwordService;

    /// <summary>Creates the administrator authentication service.</summary>
    public AdminAuthenticationService(IAdminAuthenticationContext context, IAdminPasswordService passwordService)
    {
        this.context = context;
        this.passwordService = passwordService;
    }

    /// <summary>Authenticates an active administrator and refreshes an outdated hash when required.</summary>
    public async Task<AdminLoginResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        string normalizedEmail;
        try
        {
            normalizedEmail = EmailNormalizer.Normalize(email);
        }
        catch (ArgumentException)
        {
            return AdminLoginResult.InvalidCredentials;
        }

        AdminUser? user = await context.FindByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return AdminLoginResult.InvalidCredentials;
        }

        PasswordVerificationStatus verification = passwordService.VerifyHashedPassword(user, password);
        if (verification == PasswordVerificationStatus.Failed)
        {
            return AdminLoginResult.InvalidCredentials;
        }
        if (verification == PasswordVerificationStatus.SuccessRehashNeeded)
        {
            await RehashAsync(user, password, cancellationToken);
        }

        return new AdminLoginResult(true, new AdminIdentityDto(user.Id, user.Email, user.Role));
    }

    private Task RehashAsync(AdminUser user, string password, CancellationToken cancellationToken)
    {
        return context.ExecuteInTransactionAsync<object?>(async token =>
        {
            user.ReplacePasswordHashForRehash(passwordService.HashPassword(user, password));
            await context.SaveChangesAsync(token);
            return null;
        }, cancellationToken);
    }
}

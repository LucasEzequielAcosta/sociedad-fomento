namespace SociedadFomento.Application.Authentication.Models;

/// <summary>Represents the result of password hash verification.</summary>
public enum PasswordVerificationStatus
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2
}

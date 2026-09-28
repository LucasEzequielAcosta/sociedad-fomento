using SociedadFomento.Application.Authentication.Dto;

namespace SociedadFomento.Application.Authentication.Models;

/// <summary>Represents a generic login result without exposing failure cause.</summary>
public sealed record AdminLoginResult(bool Succeeded, AdminIdentityDto? Identity)
{
    public static AdminLoginResult InvalidCredentials { get; } = new(false, null);
}

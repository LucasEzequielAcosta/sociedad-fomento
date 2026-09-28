using System.ComponentModel.DataAnnotations;
using SociedadFomento.Domain.Constants;

namespace SociedadFomento.Api.Contracts.Authentication;

/// <summary>Represents administrator login credentials.</summary>
public sealed record LoginRequest(
    [Required, EmailAddress, MaxLength(AdminUserFieldLengths.Email)] string Email,
    [Required] string Password);

using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Authentication.Dto;

/// <summary>Represents the minimum authenticated administrator identity.</summary>
public sealed record AdminIdentityDto(long Id, string Email, AdminRole Role);

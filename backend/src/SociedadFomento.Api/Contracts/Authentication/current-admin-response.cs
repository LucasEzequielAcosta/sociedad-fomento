namespace SociedadFomento.Api.Contracts.Authentication;

/// <summary>Represents the minimum current administrator identity.</summary>
public sealed record CurrentAdminResponse(long Id, string Email, string Role);

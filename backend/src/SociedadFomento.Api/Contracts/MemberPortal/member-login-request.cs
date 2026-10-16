namespace SociedadFomento.Api.Contracts.MemberPortal;

/// <summary>Represents a member portal login request identified only by DNI.</summary>
public sealed record MemberLoginRequest(string Dni);

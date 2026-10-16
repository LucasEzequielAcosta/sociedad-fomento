namespace SociedadFomento.Api.Contracts.MemberPortal;

/// <summary>Represents the authenticated member identity returned by the portal.</summary>
public sealed record CurrentMemberResponse(long MemberId, string Dni, string FirstName, string LastName, string Status);

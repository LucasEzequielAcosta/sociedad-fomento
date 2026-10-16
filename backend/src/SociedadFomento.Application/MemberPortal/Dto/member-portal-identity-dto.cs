using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.MemberPortal.Dto;

/// <summary>Represents the member identity established by a DNI portal login.</summary>
public sealed record MemberPortalIdentityDto(long MemberId, string Dni, string FirstName, string LastName, MemberStatus Status);

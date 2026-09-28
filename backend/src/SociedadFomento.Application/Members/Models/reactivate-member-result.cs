using SociedadFomento.Application.Members.Dto;

namespace SociedadFomento.Application.Members.Models;

/// <summary>Returns a reactivated member and whether a monthly obligation was created.</summary>
public sealed record ReactivateMemberResult(MemberDetailDto Member, bool ObligationCreated);

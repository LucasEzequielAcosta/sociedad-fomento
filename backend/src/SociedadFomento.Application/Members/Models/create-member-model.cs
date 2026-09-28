namespace SociedadFomento.Application.Members.Models;

/// <summary>Contains data required to register a member.</summary>
public sealed record CreateMemberModel(string Dni, MemberPersonalData PersonalData);

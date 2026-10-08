namespace SociedadFomento.Application.MemberStatements.Models;

/// <summary>Data required to produce a member account statement.</summary>
public sealed record GetMemberStatementModel(long MemberId, DateOnly AsOf);

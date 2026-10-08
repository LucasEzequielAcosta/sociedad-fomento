using SociedadFomento.Application.MemberStatements.Dto;

namespace SociedadFomento.Api.Contracts.MemberStatements;

/// <summary>Represents a member account statement returned by the API.</summary>
public sealed record MemberStatementResponse(
    long MemberId,
    string Dni,
    string FirstName,
    string LastName,
    string Status,
    IReadOnlyList<MemberStatementObligationDto> Obligations,
    IReadOnlyList<MemberStatementPaymentDto> Payments,
    decimal TotalPaid,
    decimal TotalDebt);

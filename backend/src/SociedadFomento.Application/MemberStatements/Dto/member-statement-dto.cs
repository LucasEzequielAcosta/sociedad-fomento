using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.MemberStatements.Dto;

/// <summary>Represents a member account statement with obligations, payments and debt.</summary>
public sealed record MemberStatementDto(
    long MemberId,
    string Dni,
    string FirstName,
    string LastName,
    MemberStatus Status,
    IReadOnlyList<MemberStatementObligationDto> Obligations,
    IReadOnlyList<MemberStatementPaymentDto> Payments,
    decimal TotalPaid,
    decimal TotalDebt);

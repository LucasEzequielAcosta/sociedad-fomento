using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.MemberStatements.Dto;

/// <summary>Represents a fee obligation as shown on a member statement.</summary>
public sealed record MemberStatementObligationDto(
    long Id,
    DateOnly Period,
    decimal Amount,
    FeeObligationStatus Status);

using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.MemberStatements.Dto;

/// <summary>Represents a payment as shown on a member statement.</summary>
public sealed record MemberStatementPaymentDto(
    long Id,
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status);

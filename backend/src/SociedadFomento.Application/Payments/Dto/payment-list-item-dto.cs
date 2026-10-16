using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Payments.Dto;

/// <summary>Represents a payment row in administrative listings and exports.</summary>
public sealed record PaymentListItemDto(
    long Id,
    long MemberId,
    string MemberDni,
    string MemberFirstName,
    string MemberLastName,
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    IReadOnlyList<DateOnly> Periods);

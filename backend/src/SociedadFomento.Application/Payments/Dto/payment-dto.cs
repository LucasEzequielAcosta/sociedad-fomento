using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Payments.Dto;

/// <summary>Represents a registered payment with its allocations.</summary>
public sealed record PaymentDto(
    long Id,
    long MemberId,
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    IReadOnlyList<PaymentAllocationDto> Allocations);

using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Api.Contracts.Payments;

/// <summary>Represents a request to register a payment allocated to fee obligations.</summary>
public sealed record CreatePaymentRequest(
    long MemberId,
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    IReadOnlyList<long> ObligationIds);

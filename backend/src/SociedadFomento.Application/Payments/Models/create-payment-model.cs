using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Payments.Models;

/// <summary>Data required to register a payment allocated to complete fee obligations.</summary>
public sealed record CreatePaymentModel(
    long MemberId,
    DateOnly PaymentDate,
    decimal Amount,
    PaymentMethod Method,
    IReadOnlyList<long> ObligationIds);

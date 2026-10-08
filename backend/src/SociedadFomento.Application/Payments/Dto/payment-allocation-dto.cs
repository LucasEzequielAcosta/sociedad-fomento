namespace SociedadFomento.Application.Payments.Dto;

/// <summary>Represents an allocation of a payment to a specific fee obligation.</summary>
public sealed record PaymentAllocationDto(long FeeObligationId, DateOnly Period, decimal Amount);

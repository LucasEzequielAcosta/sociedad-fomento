namespace SociedadFomento.Application.FeeRates.Dto;

/// <summary>Represents a fee rate in the fee-rate history.</summary>
public sealed record FeeRateDto(long Id, DateOnly EffectiveMonth, decimal Amount, DateTime CreatedAtUtc);

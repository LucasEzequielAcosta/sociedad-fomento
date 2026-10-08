namespace SociedadFomento.Api.Contracts.FeeRates;

/// <summary>Represents a fee-rate entry in API responses.</summary>
public sealed record FeeRateResponse(long Id, DateOnly EffectiveMonth, decimal Amount, DateTime CreatedAtUtc);

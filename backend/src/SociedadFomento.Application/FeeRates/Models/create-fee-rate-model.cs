namespace SociedadFomento.Application.FeeRates.Models;

/// <summary>Data required to create a new fee-rate entry.</summary>
public sealed record CreateFeeRateModel(DateOnly EffectiveMonth, decimal Amount);

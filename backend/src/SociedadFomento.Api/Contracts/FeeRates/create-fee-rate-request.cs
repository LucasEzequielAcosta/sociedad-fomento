using System.ComponentModel.DataAnnotations;

namespace SociedadFomento.Api.Contracts.FeeRates;

/// <summary>Represents a request to create a new fee-rate entry.</summary>
public sealed record CreateFeeRateRequest(
    DateOnly EffectiveMonth,
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "El importe debe ser mayor a cero.")] decimal Amount);

namespace SociedadFomento.Api.Contracts.FeeObligations;

/// <summary>Represents the result of a fee-obligation generation request.</summary>
public sealed record FeeGenerationResponse(DateOnly Month, int GeneratedCount);

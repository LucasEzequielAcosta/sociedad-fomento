namespace SociedadFomento.Api.Contracts.FeeObligations;

/// <summary>Represents a request to generate fee obligations for a specific month.</summary>
public sealed record GenerateFeeObligationsRequest(DateOnly Month);

namespace SociedadFomento.Application.FeeGeneration.Dto;

/// <summary>Result of generating monthly fee obligations for a given month.</summary>
public sealed record FeeGenerationResult(DateOnly Month, int GeneratedCount);

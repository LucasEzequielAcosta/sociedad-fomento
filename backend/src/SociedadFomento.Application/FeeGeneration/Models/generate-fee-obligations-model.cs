namespace SociedadFomento.Application.FeeGeneration.Models;

/// <summary>Data required to generate fee obligations for a specific month.</summary>
public sealed record GenerateFeeObligationsModel(DateOnly Month);

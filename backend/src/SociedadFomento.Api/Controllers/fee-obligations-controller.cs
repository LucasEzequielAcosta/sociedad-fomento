using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SociedadFomento.Api.Contracts.FeeObligations;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.FeeGeneration;
using SociedadFomento.Application.FeeGeneration.Dto;
using SociedadFomento.Application.FeeGeneration.Models;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes administrative fee-obligation operations.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/fee-obligations")]
public sealed class FeeObligationsController : ControllerBase
{
    private readonly FeeGenerationService feeGenerationService;
    private readonly IClock clock;

    /// <summary>Creates the fee-obligations controller.</summary>
    public FeeObligationsController(FeeGenerationService feeGenerationService, IClock clock)
    {
        this.feeGenerationService = feeGenerationService;
        this.clock = clock;
    }

    /// <summary>Generates fee obligations for active members for the supplied month.</summary>
    [HttpPost("generate")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<FeeGenerationResponse>> Generate(
        GenerateFeeObligationsRequest request, CancellationToken cancellationToken)
    {
        FeeGenerationResult result = await feeGenerationService.GenerateAsync(
            new GenerateFeeObligationsModel(request.Month), clock.UtcNow, cancellationToken);
        return Ok(new FeeGenerationResponse(result.Month, result.GeneratedCount));
    }
}

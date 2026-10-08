using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SociedadFomento.Api.Contracts.FeeRates;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.FeeRates;
using SociedadFomento.Application.FeeRates.Dto;
using SociedadFomento.Application.FeeRates.Models;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes administrative fee-rate operations.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/fee-rates")]
public sealed class FeeRatesController : ControllerBase
{
    private readonly FeeRateService feeRateService;
    private readonly IClock clock;

    /// <summary>Creates the fee-rates controller.</summary>
    public FeeRatesController(FeeRateService feeRateService, IClock clock)
    {
        this.feeRateService = feeRateService;
        this.clock = clock;
    }

    /// <summary>Creates a new fee-rate entry.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<FeeRateResponse>> Create(CreateFeeRateRequest request, CancellationToken cancellationToken)
    {
        FeeRateDto feeRate = await feeRateService.CreateAsync(
            new CreateFeeRateModel(request.EffectiveMonth, request.Amount), clock.UtcNow, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = feeRate.Id }, ToResponse(feeRate));
    }

    /// <summary>Returns all fee-rate entries ordered by effective month.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FeeRateResponse>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<FeeRateDto> rates = await feeRateService.GetAllAsync(cancellationToken);
        return Ok(rates.Select(ToResponse).ToList());
    }

    /// <summary>Returns the fee-rate applicable for the current month.</summary>
    [HttpGet("current")]
    public async Task<ActionResult<FeeRateResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        return Ok(ToResponse(await feeRateService.GetApplicableAsync(clock.Today, cancellationToken)));
    }

    /// <summary>Returns the fee-rate applicable for the supplied month.</summary>
    [HttpGet("applicable")]
    public async Task<ActionResult<FeeRateResponse>> GetApplicable(DateOnly month, CancellationToken cancellationToken)
    {
        return Ok(ToResponse(await feeRateService.GetApplicableAsync(month, cancellationToken)));
    }

    /// <summary>Returns a fee-rate entry by identifier.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<FeeRateResponse>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(ToResponse(await feeRateService.GetByIdAsync(id, cancellationToken)));
    }

    private static FeeRateResponse ToResponse(FeeRateDto dto) =>
        new(dto.Id, dto.EffectiveMonth, dto.Amount, dto.CreatedAtUtc);
}

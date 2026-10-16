using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.FeeRates.Dto;
using SociedadFomento.Application.FeeRates.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.FeeRates;

/// <summary>Coordinates administrative fee-rate use cases.</summary>
public sealed class FeeRateService
{
    private readonly IFeeRateContext context;

    /// <summary>Creates the fee-rate application service.</summary>
    public FeeRateService(IFeeRateContext context)
    {
        this.context = context;
    }

    /// <summary>Creates a new fee-rate entry for the supplied month.</summary>
    public async Task<FeeRateDto> CreateAsync(CreateFeeRateModel model, DateTime createdAtUtc, CancellationToken cancellationToken = default)
    {
        DateOnly effectiveMonth = NormalizeMonth(model.EffectiveMonth);
        if (await context.ExistsForMonthAsync(effectiveMonth, cancellationToken))
        {
            throw new BusinessRuleException("FEE_RATE_MONTH_DUPLICATE", "Ya existe un valor de cuota para el mes indicado.");
        }

        FeeRate feeRate;
        try
        {
            feeRate = new FeeRate(effectiveMonth, model.Amount, createdAtUtc);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new RequestValidationException("INVALID_FEE_RATE_AMOUNT", exception.Message);
        }

        context.Add(feeRate);
        await context.SaveChangesAsync(cancellationToken);
        return Map(feeRate);
    }

    /// <summary>Returns a fee-rate entry by identifier.</summary>
    public async Task<FeeRateDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        FeeRateDto? feeRate = await context.GetByIdAsync(id, cancellationToken);
        return feeRate ?? throw new ResourceNotFoundException("FEE_RATE_NOT_FOUND", "No se encontró el valor de cuota.");
    }

    /// <summary>Returns the complete fee-rate history ordered by effective month.</summary>
    public Task<IReadOnlyList<FeeRateDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        context.GetAllAsync(cancellationToken);

    /// <summary>Returns the fee rate applicable for the supplied period.</summary>
    public async Task<FeeRateDto> GetApplicableAsync(DateOnly period, CancellationToken cancellationToken = default)
    {
        FeeRate? feeRate = await context.FindApplicableRateAsync(NormalizeMonth(period), cancellationToken);
        return feeRate is null
            ? throw new ResourceNotFoundException("FEE_RATE_NOT_FOUND", "No hay un valor de cuota vigente para el mes indicado.")
            : Map(feeRate);
    }

    private static DateOnly NormalizeMonth(DateOnly value) => new(value.Year, value.Month, 1);

    private static FeeRateDto Map(FeeRate feeRate) =>
        new(feeRate.Id, feeRate.EffectiveMonth, feeRate.Amount, feeRate.CreatedAtUtc);
}

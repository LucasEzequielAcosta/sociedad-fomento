using SociedadFomento.Application.FeeRates;
using SociedadFomento.Application.FeeRates.Dto;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

internal sealed class FakeFeeRateContext : IFeeRateContext
{
    internal List<FeeRate> FeeRates { get; } = [];

    public Task<bool> ExistsForMonthAsync(DateOnly effectiveMonth, CancellationToken cancellationToken) =>
        Task.FromResult(FeeRates.Any(rate => rate.EffectiveMonth == effectiveMonth));

    public Task<FeeRate?> FindApplicableRateAsync(DateOnly period, CancellationToken cancellationToken) =>
        Task.FromResult(FeeRates.Where(rate => rate.EffectiveMonth <= period)
            .OrderByDescending(rate => rate.EffectiveMonth).FirstOrDefault());

    public Task<FeeRateDto?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(FeeRates.Where(rate => rate.Id == id)
            .Select(rate => new FeeRateDto(rate.Id, rate.EffectiveMonth, rate.Amount, rate.CreatedAtUtc))
            .SingleOrDefault());

    public Task<IReadOnlyList<FeeRateDto>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FeeRateDto>>(FeeRates
            .OrderBy(rate => rate.EffectiveMonth)
            .Select(rate => new FeeRateDto(rate.Id, rate.EffectiveMonth, rate.Amount, rate.CreatedAtUtc))
            .ToList());

    public void Add(FeeRate feeRate) => FeeRates.Add(feeRate);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.FeeRates;
using SociedadFomento.Application.FeeRates.Dto;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.FeeRates;

internal sealed class FeeRateContext : IFeeRateContext
{
    private readonly SociedadFomentoDbContext dbContext;

    public FeeRateContext(SociedadFomentoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<bool> ExistsForMonthAsync(DateOnly effectiveMonth, CancellationToken cancellationToken) =>
        dbContext.FeeRates.AnyAsync(rate => rate.EffectiveMonth == effectiveMonth, cancellationToken);

    public Task<FeeRate?> FindApplicableRateAsync(DateOnly period, CancellationToken cancellationToken) =>
        dbContext.FeeRates.Where(rate => rate.EffectiveMonth <= period)
            .OrderByDescending(rate => rate.EffectiveMonth).FirstOrDefaultAsync(cancellationToken);

    public Task<FeeRateDto?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        dbContext.FeeRates.AsNoTracking().Where(rate => rate.Id == id)
            .Select(rate => new FeeRateDto(rate.Id, rate.EffectiveMonth, rate.Amount, rate.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<FeeRateDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<FeeRateDto> rates = await dbContext.FeeRates.AsNoTracking()
            .OrderBy(rate => rate.EffectiveMonth)
            .Select(rate => new FeeRateDto(rate.Id, rate.EffectiveMonth, rate.Amount, rate.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return rates;
    }

    public void Add(FeeRate feeRate) => dbContext.FeeRates.Add(feeRate);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueMonthConflict(exception))
        {
            throw new BusinessRuleException("FEE_RATE_MONTH_DUPLICATE", "A fee rate already exists for the supplied month.");
        }
    }

    private static bool IsUniqueMonthConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException &&
        sqlException.Number is 2601 or 2627 &&
        sqlException.Message.Contains("IX_FeeRates_EffectiveMonth", StringComparison.OrdinalIgnoreCase);
}

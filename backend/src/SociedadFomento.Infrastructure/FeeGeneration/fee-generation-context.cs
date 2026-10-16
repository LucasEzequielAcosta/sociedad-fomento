using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.FeeGeneration;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.FeeGeneration;

internal sealed class FeeGenerationContext : IFeeGenerationContext
{
    private readonly SociedadFomentoDbContext dbContext;

    public FeeGenerationContext(SociedadFomentoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<FeeRate?> FindApplicableFeeRateAsync(DateOnly period, CancellationToken cancellationToken) =>
        dbContext.FeeRates.Where(rate => rate.EffectiveMonth <= period)
            .OrderByDescending(rate => rate.EffectiveMonth).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MembershipPeriod>> GetPeriodsCoveringMonthAsync(DateOnly month, CancellationToken cancellationToken)
    {
        DateOnly firstDay = new(month.Year, month.Month, 1);
        DateOnly lastDay = firstDay.AddMonths(1).AddDays(-1);
        List<MembershipPeriod> periods = await dbContext.MembershipPeriods
            .Include(period => period.Member)
            .Where(period => period.StartDate <= lastDay && (!period.EndDate.HasValue || period.EndDate.Value >= firstDay))
            .ToListAsync(cancellationToken);
        return periods;
    }

    public async Task<HashSet<long>> GetMembersWithObligationAsync(DateOnly month, CancellationToken cancellationToken)
    {
        DateOnly normalized = new(month.Year, month.Month, 1);
        List<long> memberIds = await dbContext.FeeObligations
            .Where(obligation => obligation.Period == normalized)
            .Select(obligation => obligation.MemberId)
            .ToListAsync(cancellationToken);
        return new HashSet<long>(memberIds);
    }

    public void AddObligation(FeeObligation obligation) => dbContext.FeeObligations.Add(obligation);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsMemberObligationConflict(exception))
        {
            throw new BusinessRuleException("FEE_OBLIGATION_DUPLICATE", "Ya existe una cuota para alguno de los socios en el mes indicado.");
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            T result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static bool IsMemberObligationConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException &&
        sqlException.Number is 2601 or 2627 &&
        sqlException.Message.Contains("IX_FeeObligations_MemberId_Period", StringComparison.OrdinalIgnoreCase);
}

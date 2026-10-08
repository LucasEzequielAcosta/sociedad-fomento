using SociedadFomento.Application.FeeGeneration;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

internal sealed class FakeFeeGenerationContext : IFeeGenerationContext
{
    internal List<FeeRate> FeeRates { get; } = [];
    internal List<MembershipPeriod> MembershipPeriods { get; } = [];
    internal List<FeeObligation> Obligations { get; } = [];

    public Task<FeeRate?> FindApplicableFeeRateAsync(DateOnly period, CancellationToken cancellationToken) =>
        Task.FromResult(FeeRates.Where(rate => rate.EffectiveMonth <= period)
            .OrderByDescending(rate => rate.EffectiveMonth).FirstOrDefault());

    public Task<IReadOnlyList<MembershipPeriod>> GetPeriodsCoveringMonthAsync(DateOnly month, CancellationToken cancellationToken)
    {
        DateOnly firstDay = new(month.Year, month.Month, 1);
        DateOnly lastDay = firstDay.AddMonths(1).AddDays(-1);
        List<MembershipPeriod> periods = MembershipPeriods
            .Where(period => period.StartDate <= lastDay && (!period.EndDate.HasValue || period.EndDate.Value >= firstDay))
            .ToList();
        return Task.FromResult<IReadOnlyList<MembershipPeriod>>(periods);
    }

    public Task<HashSet<long>> GetMembersWithObligationAsync(DateOnly month, CancellationToken cancellationToken)
    {
        DateOnly normalized = new(month.Year, month.Month, 1);
        HashSet<long> memberIds = new(Obligations.Where(obligation => obligation.Period == normalized).Select(obligation => obligation.MemberId));
        return Task.FromResult(memberIds);
    }

    public void AddObligation(FeeObligation obligation) => Obligations.Add(obligation);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        operation(cancellationToken);
}

using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.FeeGeneration;

/// <summary>Defines persistence operations required to generate monthly fee obligations.</summary>
public interface IFeeGenerationContext
{
    Task<FeeRate?> FindApplicableFeeRateAsync(DateOnly period, CancellationToken cancellationToken);
    Task<IReadOnlyList<MembershipPeriod>> GetPeriodsCoveringMonthAsync(DateOnly month, CancellationToken cancellationToken);
    Task<HashSet<long>> GetMembersWithObligationAsync(DateOnly month, CancellationToken cancellationToken);
    void AddObligation(FeeObligation obligation);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}

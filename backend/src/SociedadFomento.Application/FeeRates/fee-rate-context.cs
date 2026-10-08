using SociedadFomento.Application.FeeRates.Dto;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.FeeRates;

/// <summary>Defines fee-rate persistence operations required by the application service.</summary>
public interface IFeeRateContext
{
    Task<bool> ExistsForMonthAsync(DateOnly effectiveMonth, CancellationToken cancellationToken);
    Task<FeeRate?> FindApplicableRateAsync(DateOnly period, CancellationToken cancellationToken);
    Task<FeeRateDto?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<IReadOnlyList<FeeRateDto>> GetAllAsync(CancellationToken cancellationToken);
    void Add(FeeRate feeRate);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

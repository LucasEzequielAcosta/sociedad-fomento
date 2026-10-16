using SociedadFomento.Application.Accounting.Dto;
using SociedadFomento.Application.Accounting.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Accounting;

/// <summary>Defines accounting-entry persistence operations required by the application service.</summary>
public interface IAccountingEntryContext
{
    /// <summary>Loads a tracked accounting entry by identifier.</summary>
    Task<AccountingEntry?> FindByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>Returns an accounting entry projection by identifier.</summary>
    Task<AccountingEntryDto?> GetByIdAsync(long id, CancellationToken cancellationToken);

    /// <summary>Returns entries matching the supplied filters, ordered by most recent first.</summary>
    Task<IReadOnlyList<AccountingEntryDto>> GetEntriesAsync(AccountingEntryFilter filter, CancellationToken cancellationToken);

    /// <summary>Returns active income and expense totals for the inclusive date range.</summary>
    Task<AccountingSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);

    /// <summary>Stages a new accounting entry for persistence.</summary>
    void Add(AccountingEntry entry);

    /// <summary>Persists tracked changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

using SociedadFomento.Application.Accounting;
using SociedadFomento.Application.Accounting.Dto;
using SociedadFomento.Application.Accounting.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Tests;

internal sealed class FakeAccountingEntryContext : IAccountingEntryContext
{
    internal List<AccountingEntry> Entries { get; } = [];

    public Task<AccountingEntry?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.SingleOrDefault(entry => entry.Id == id));

    public Task<AccountingEntryDto?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        Task.FromResult(Entries.Where(entry => entry.Id == id).Select(Map).SingleOrDefault());

    public Task<IReadOnlyList<AccountingEntryDto>> GetEntriesAsync(AccountingEntryFilter filter, CancellationToken cancellationToken)
    {
        IEnumerable<AccountingEntry> query = Entries;
        if (filter.From is not null)
        {
            query = query.Where(entry => entry.EntryDate >= filter.From.Value);
        }

        if (filter.To is not null)
        {
            query = query.Where(entry => entry.EntryDate <= filter.To.Value);
        }

        if (filter.Type is not null)
        {
            query = query.Where(entry => entry.Type == filter.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Concept))
        {
            string concept = filter.Concept.Trim();
            query = query.Where(entry => entry.Concept.Contains(concept, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<AccountingEntryDto>>(
            query.OrderByDescending(entry => entry.EntryDate).ThenByDescending(entry => entry.Id).Select(Map).ToList());
    }

    public Task<AccountingSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        List<AccountingEntry> active = Entries
            .Where(entry => entry.Status == AccountingEntryStatus.Active && entry.EntryDate >= from && entry.EntryDate <= to)
            .ToList();
        decimal income = active.Where(entry => entry.Type == AccountingEntryType.Income).Sum(entry => entry.Amount);
        decimal expense = active.Where(entry => entry.Type == AccountingEntryType.Expense).Sum(entry => entry.Amount);
        return Task.FromResult(new AccountingSummaryDto(from, to, income, expense, income - expense));
    }

    public void Add(AccountingEntry entry) => Entries.Add(entry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static AccountingEntryDto Map(AccountingEntry entry) => new(
        entry.Id, entry.EntryDate, entry.Type, entry.Source, entry.Concept, entry.Amount,
        entry.Status, entry.PaymentId, entry.CancelledAtUtc, entry.CreatedAtUtc);
}

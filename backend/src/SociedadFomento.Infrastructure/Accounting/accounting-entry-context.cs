using Microsoft.EntityFrameworkCore;
using SociedadFomento.Application.Accounting;
using SociedadFomento.Application.Accounting.Dto;
using SociedadFomento.Application.Accounting.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Accounting;

internal sealed class AccountingEntryContext : IAccountingEntryContext
{
    private readonly SociedadFomentoDbContext dbContext;

    public AccountingEntryContext(SociedadFomentoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<AccountingEntry?> FindByIdAsync(long id, CancellationToken cancellationToken) =>
        dbContext.AccountingEntries.SingleOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    public Task<AccountingEntryDto?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        dbContext.AccountingEntries.AsNoTracking().Where(entry => entry.Id == id)
            .Select(entry => new AccountingEntryDto(
                entry.Id, entry.EntryDate, entry.Type, entry.Source, entry.Concept, entry.Amount,
                entry.Status, entry.PaymentId, entry.CancelledAtUtc, entry.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AccountingEntryDto>> GetEntriesAsync(AccountingEntryFilter filter, CancellationToken cancellationToken)
    {
        IQueryable<AccountingEntry> query = dbContext.AccountingEntries.AsNoTracking();
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
            query = query.Where(entry => entry.Concept.Contains(concept));
        }

        List<AccountingEntryDto> entries = await query
            .OrderByDescending(entry => entry.EntryDate)
            .ThenByDescending(entry => entry.Id)
            .Select(entry => new AccountingEntryDto(
                entry.Id, entry.EntryDate, entry.Type, entry.Source, entry.Concept, entry.Amount,
                entry.Status, entry.PaymentId, entry.CancelledAtUtc, entry.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return entries;
    }

    public async Task<AccountingSummaryDto> GetSummaryAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        List<EntryTotal> totals = await dbContext.AccountingEntries.AsNoTracking()
            .Where(entry => entry.Status == AccountingEntryStatus.Active && entry.EntryDate >= from && entry.EntryDate <= to)
            .GroupBy(entry => entry.Type)
            .Select(group => new EntryTotal(group.Key, group.Sum(entry => entry.Amount)))
            .ToListAsync(cancellationToken);

        decimal totalIncome = totals.Where(total => total.Type == AccountingEntryType.Income).Sum(total => total.Amount);
        decimal totalExpense = totals.Where(total => total.Type == AccountingEntryType.Expense).Sum(total => total.Amount);
        return new AccountingSummaryDto(from, to, totalIncome, totalExpense, totalIncome - totalExpense);
    }

    public void Add(AccountingEntry entry) => dbContext.AccountingEntries.Add(entry);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    private sealed record EntryTotal(AccountingEntryType Type, decimal Amount);
}

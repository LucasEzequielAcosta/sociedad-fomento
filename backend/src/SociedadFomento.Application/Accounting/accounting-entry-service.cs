using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.Accounting.Dto;
using SociedadFomento.Application.Accounting.Models;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Accounting;

/// <summary>Coordinates manual income/expense registration, cancellation, listing and summary use cases.</summary>
public sealed class AccountingEntryService
{
    private readonly IAccountingEntryContext context;
    private readonly IClock clock;

    /// <summary>Creates the accounting-entry application service.</summary>
    public AccountingEntryService(IAccountingEntryContext context, IClock clock)
    {
        this.context = context;
        this.clock = clock;
    }

    /// <summary>Registers a manual income or expense.</summary>
    public async Task<AccountingEntryDto> CreateManualAsync(
        CreateManualAccountingEntryModel model, DateTime createdAtUtc, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(model.Type))
        {
            throw new RequestValidationException("INVALID_ACCOUNTING_ENTRY_TYPE", "The entry type must be income or expense.");
        }

        AccountingEntry entry;
        try
        {
            entry = AccountingEntry.CreateManual(model.EntryDate, model.Type, model.Concept, model.Amount, createdAtUtc);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new RequestValidationException("INVALID_ACCOUNTING_ENTRY_AMOUNT", exception.Message);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException("INVALID_ACCOUNTING_ENTRY_CONCEPT", exception.Message);
        }

        context.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
        return Map(entry);
    }

    /// <summary>Cancels a manual entry while preserving its history.</summary>
    public async Task CancelAsync(long id, DateTime cancelledAtUtc, CancellationToken cancellationToken = default)
    {
        AccountingEntry entry = await context.FindByIdAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("ACCOUNTING_ENTRY_NOT_FOUND", "Accounting entry not found.");

        if (entry.Source != AccountingEntrySource.Manual)
        {
            throw new BusinessRuleException("ACCOUNTING_ENTRY_NOT_MANUAL",
                "Automatic entries can only be cancelled by cancelling the originating payment.");
        }

        try
        {
            entry.Cancel(cancelledAtUtc);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessRuleException("ACCOUNTING_ENTRY_ALREADY_CANCELLED", exception.Message);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Returns an accounting entry by identifier.</summary>
    public async Task<AccountingEntryDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        AccountingEntryDto? entry = await context.GetByIdAsync(id, cancellationToken);
        return entry ?? throw new ResourceNotFoundException("ACCOUNTING_ENTRY_NOT_FOUND", "Accounting entry not found.");
    }

    /// <summary>Returns accounting entries matching the supplied filters.</summary>
    public async Task<IReadOnlyList<AccountingEntryDto>> GetEntriesAsync(
        AccountingEntryFilter filter, CancellationToken cancellationToken = default)
    {
        ValidateRange(filter.From, filter.To);
        return await context.GetEntriesAsync(filter, cancellationToken);
    }

    /// <summary>Returns active income, expense and balance totals for the requested period.</summary>
    public async Task<AccountingSummaryDto> GetSummaryAsync(
        GetAccountingSummaryModel model, CancellationToken cancellationToken = default)
    {
        (DateOnly from, DateOnly to) = ResolveRange(model);
        return await context.GetSummaryAsync(from, to, cancellationToken);
    }

    private (DateOnly From, DateOnly To) ResolveRange(GetAccountingSummaryModel model)
    {
        if (model.From is not null || model.To is not null)
        {
            ValidateRange(model.From, model.To);
            return (model.From!.Value, model.To!.Value);
        }

        DateOnly today = clock.Today;
        return model.Period switch
        {
            null or AccountingSummaryPeriod.CurrentMonth => MonthRange(today),
            AccountingSummaryPeriod.PreviousMonth => MonthRange(today.AddMonths(-1)),
            AccountingSummaryPeriod.Year => (new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31)),
            _ => throw new RequestValidationException("INVALID_ACCOUNTING_PERIOD", "The requested accounting period is not supported."),
        };
    }

    private static (DateOnly From, DateOnly To) MonthRange(DateOnly month) =>
        (new DateOnly(month.Year, month.Month, 1), new DateOnly(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month)));

    private static void ValidateRange(DateOnly? from, DateOnly? to)
    {
        if (from is null && to is null)
        {
            return;
        }

        if (from is null || to is null || from > to)
        {
            throw new RequestValidationException("INVALID_ACCOUNTING_DATE_RANGE",
                "Both dates are required and the start date cannot be after the end date.");
        }
    }

    private static AccountingEntryDto Map(AccountingEntry entry) => new(
        entry.Id, entry.EntryDate, entry.Type, entry.Source, entry.Concept, entry.Amount,
        entry.Status, entry.PaymentId, entry.CancelledAtUtc, entry.CreatedAtUtc);
}

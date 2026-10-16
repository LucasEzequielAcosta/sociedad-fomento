using SociedadFomento.Application.Accounting;
using SociedadFomento.Application.Accounting.Dto;
using SociedadFomento.Application.Accounting.Models;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests manual accounting-entry workflows and summary calculations.</summary>
public sealed class AccountingEntryServiceTests
{
    private static readonly DateTime CreatedAtUtc = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Verifies that a manual income is stored and returned.</summary>
    [Fact]
    public async Task CreateManual_StoresIncome()
    {
        (AccountingEntryService service, FakeAccountingEntryContext context) = CreateService();

        AccountingEntryDto result = await service.CreateManualAsync(
            new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 3000m), CreatedAtUtc);

        Assert.Equal(AccountingEntryType.Income, result.Type);
        Assert.Equal(AccountingEntrySource.Manual, result.Source);
        Assert.Equal(3000m, result.Amount);
        Assert.Single(context.Entries);
    }

    /// <summary>Verifies that a manual expense is stored and returned.</summary>
    [Fact]
    public async Task CreateManual_StoresExpense()
    {
        (AccountingEntryService service, FakeAccountingEntryContext context) = CreateService();

        AccountingEntryDto result = await service.CreateManualAsync(
            new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), AccountingEntryType.Expense, "Limpieza", 1500m), CreatedAtUtc);

        Assert.Equal(AccountingEntryType.Expense, result.Type);
        Assert.Null(result.PaymentId);
        Assert.Single(context.Entries);
    }

    /// <summary>Verifies that a nonpositive amount is rejected.</summary>
    [Fact]
    public async Task CreateManual_RejectsNonPositiveAmount()
    {
        (AccountingEntryService service, _) = CreateService();

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateManualAsync(
                new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 0m), CreatedAtUtc));

        Assert.Equal("INVALID_ACCOUNTING_ENTRY_AMOUNT", exception.Code);
    }

    /// <summary>Verifies that an empty concept is rejected.</summary>
    [Fact]
    public async Task CreateManual_RejectsEmptyConcept()
    {
        (AccountingEntryService service, _) = CreateService();

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateManualAsync(
                new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "   ", 1000m), CreatedAtUtc));

        Assert.Equal("INVALID_ACCOUNTING_ENTRY_CONCEPT", exception.Code);
    }

    /// <summary>Verifies that an undefined entry type is rejected.</summary>
    [Fact]
    public async Task CreateManual_RejectsInvalidType()
    {
        (AccountingEntryService service, _) = CreateService();

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateManualAsync(
                new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), (AccountingEntryType)99, "Otro", 1000m), CreatedAtUtc));

        Assert.Equal("INVALID_ACCOUNTING_ENTRY_TYPE", exception.Code);
    }

    /// <summary>Verifies that cancelling a manual entry marks it cancelled.</summary>
    [Fact]
    public async Task Cancel_MarksManualEntryCancelled()
    {
        (AccountingEntryService service, FakeAccountingEntryContext context) = CreateService();
        AccountingEntry entry = AccountingEntry.CreateManual(
            new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 3000m, CreatedAtUtc);
        context.Entries.Add(entry);

        await service.CancelAsync(entry.Id, CreatedAtUtc.AddDays(1));

        Assert.Equal(AccountingEntryStatus.Cancelled, entry.Status);
        Assert.Equal(CreatedAtUtc.AddDays(1), entry.CancelledAtUtc);
    }

    /// <summary>Verifies that cancelling a missing entry throws.</summary>
    [Fact]
    public async Task Cancel_MissingEntryThrowsNotFound()
    {
        (AccountingEntryService service, _) = CreateService();

        ResourceNotFoundException exception = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.CancelAsync(42, CreatedAtUtc));

        Assert.Equal("ACCOUNTING_ENTRY_NOT_FOUND", exception.Code);
    }

    /// <summary>Verifies that an automatic payment entry cannot be cancelled directly.</summary>
    [Fact]
    public async Task Cancel_RejectsAutomaticPaymentEntry()
    {
        (AccountingEntryService service, FakeAccountingEntryContext context) = CreateService();
        Member member = new("12345678", "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null, new DateOnly(2026, 9, 1));
        FeeRate rate = new(new DateOnly(2026, 9, 1), 5000m, CreatedAtUtc);
        FeeObligation obligation = new(member, member.MembershipPeriods.Single(), rate, new DateOnly(2026, 9, 1), CreatedAtUtc);
        Payment payment = new(member, new DateOnly(2026, 9, 25), 5000m, PaymentMethod.Cash, [obligation], CreatedAtUtc);
        AccountingEntry entry = AccountingEntry.CreateForPayment(payment, "Pago de cuota", CreatedAtUtc);
        context.Entries.Add(entry);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CancelAsync(entry.Id, CreatedAtUtc));

        Assert.Equal("ACCOUNTING_ENTRY_NOT_MANUAL", exception.Code);
    }

    /// <summary>Verifies that cancelling an already cancelled entry throws.</summary>
    [Fact]
    public async Task Cancel_RejectsAlreadyCancelledEntry()
    {
        (AccountingEntryService service, FakeAccountingEntryContext context) = CreateService();
        AccountingEntry entry = AccountingEntry.CreateManual(
            new DateOnly(2026, 10, 5), AccountingEntryType.Expense, "Limpieza", 1500m, CreatedAtUtc);
        entry.Cancel(CreatedAtUtc);
        context.Entries.Add(entry);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CancelAsync(entry.Id, CreatedAtUtc.AddDays(1)));

        Assert.Equal("ACCOUNTING_ENTRY_ALREADY_CANCELLED", exception.Code);
    }

    /// <summary>Verifies that an incomplete or inverted date range is rejected.</summary>
    [Fact]
    public async Task GetEntries_RejectsInvalidRange()
    {
        (AccountingEntryService service, _) = CreateService();

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.GetEntriesAsync(new AccountingEntryFilter(new DateOnly(2026, 10, 1), null, null, null)));
        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.GetEntriesAsync(new AccountingEntryFilter(new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 1), null, null)));
    }

    /// <summary>Verifies that the current-month summary covers exactly that month.</summary>
    [Fact]
    public async Task GetSummary_CurrentMonthResolvesToMonthRange()
    {
        FixedClock clock = new(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
        AccountingEntryService service = CreateService(clock).Service;

        AccountingSummaryDto summary = await service.GetSummaryAsync(new GetAccountingSummaryModel(AccountingSummaryPeriod.CurrentMonth, null, null));

        Assert.Equal(new DateOnly(2026, 10, 1), summary.From);
        Assert.Equal(new DateOnly(2026, 10, 31), summary.To);
    }

    /// <summary>Verifies that the previous-month summary resolves correctly across boundaries.</summary>
    [Fact]
    public async Task GetSummary_PreviousMonthResolvesToMonthRange()
    {
        FixedClock clock = new(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
        AccountingEntryService service = CreateService(clock).Service;

        AccountingSummaryDto summary = await service.GetSummaryAsync(new GetAccountingSummaryModel(AccountingSummaryPeriod.PreviousMonth, null, null));

        Assert.Equal(new DateOnly(2026, 9, 1), summary.From);
        Assert.Equal(new DateOnly(2026, 9, 30), summary.To);
    }

    /// <summary>Verifies that the year summary covers the full calendar year.</summary>
    [Fact]
    public async Task GetSummary_YearResolvesToYearRange()
    {
        FixedClock clock = new(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
        AccountingEntryService service = CreateService(clock).Service;

        AccountingSummaryDto summary = await service.GetSummaryAsync(new GetAccountingSummaryModel(AccountingSummaryPeriod.Year, null, null));

        Assert.Equal(new DateOnly(2026, 1, 1), summary.From);
        Assert.Equal(new DateOnly(2026, 12, 31), summary.To);
    }

    /// <summary>Verifies that an explicit range takes precedence and validates order.</summary>
    [Fact]
    public async Task GetSummary_ExplicitRangeIsUsed()
    {
        (AccountingEntryService service, _) = CreateService();

        AccountingSummaryDto summary = await service.GetSummaryAsync(
            new GetAccountingSummaryModel(AccountingSummaryPeriod.Year, new DateOnly(2026, 5, 10), new DateOnly(2026, 5, 20)));

        Assert.Equal(new DateOnly(2026, 5, 10), summary.From);
        Assert.Equal(new DateOnly(2026, 5, 20), summary.To);
    }

    /// <summary>Verifies that the summary totals only active entries and computes the balance.</summary>
    [Fact]
    public async Task GetSummary_ExcludesCancelledEntriesAndComputesBalance()
    {
        (AccountingEntryService service, FakeAccountingEntryContext context) = CreateService();
        context.Entries.Add(AccountingEntry.CreateManual(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 3000m, CreatedAtUtc));
        context.Entries.Add(AccountingEntry.CreateManual(new DateOnly(2026, 10, 6), AccountingEntryType.Expense, "Limpieza", 1000m, CreatedAtUtc));
        AccountingEntry cancelled = AccountingEntry.CreateManual(new DateOnly(2026, 10, 7), AccountingEntryType.Income, "Anulado", 9000m, CreatedAtUtc);
        cancelled.Cancel(CreatedAtUtc);
        context.Entries.Add(cancelled);
        context.Entries.Add(AccountingEntry.CreateManual(new DateOnly(2026, 11, 1), AccountingEntryType.Income, "Fuera de rango", 5000m, CreatedAtUtc));

        AccountingSummaryDto summary = await service.GetSummaryAsync(new GetAccountingSummaryModel(null, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));

        Assert.Equal(3000m, summary.TotalIncome);
        Assert.Equal(1000m, summary.TotalExpense);
        Assert.Equal(2000m, summary.Balance);
    }

    private static (AccountingEntryService Service, FakeAccountingEntryContext Context) CreateService(FixedClock? clock = null)
    {
        FakeAccountingEntryContext context = new();
        return (new AccountingEntryService(context, clock ?? new FixedClock(CreatedAtUtc)), context);
    }
}

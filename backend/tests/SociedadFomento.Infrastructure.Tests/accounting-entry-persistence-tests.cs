using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Accounting;
using SociedadFomento.Application.Accounting.Dto;
using SociedadFomento.Application.Accounting.Models;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies accounting-entry persistence and summary behavior against SQL Server.</summary>
public sealed class AccountingEntryPersistenceTests : IAsyncLifetime
{
    private readonly string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoAccountingTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
    private readonly FixedClock clock = new(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
    private ServiceProvider services = null!;

    /// <summary>Creates and migrates the isolated database.</summary>
    public async Task InitializeAsync()
    {
        ServiceCollection serviceCollection = new();
        serviceCollection.AddInfrastructure(connectionString);
        serviceCollection.AddSingleton<SociedadFomento.Application.Abstractions.IClock>(clock);
        services = serviceCollection.BuildServiceProvider();
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>Deletes the isolated database.</summary>
    public async Task DisposeAsync()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.EnsureDeletedAsync();
        await services.DisposeAsync();
    }

    /// <summary>Verifies that a manual income is persisted with its manual source.</summary>
    [Fact]
    public async Task CreateManual_PersistsIncome()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AccountingEntryService service = scope.ServiceProvider.GetRequiredService<AccountingEntryService>();

        AccountingEntryDto result = await service.CreateManualAsync(
            new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 3000m), clock.UtcNow);

        Assert.True(result.Id > 0);
        Assert.Equal(AccountingEntrySource.Manual, result.Source);
        Assert.Equal(AccountingEntryStatus.Active, result.Status);

        AccountingEntryDto persisted = await service.GetByIdAsync(result.Id);
        Assert.Equal("Donación", persisted.Concept);
        Assert.Equal(3000m, persisted.Amount);
    }

    /// <summary>Verifies that cancelling a manual entry preserves the row and excludes it from totals.</summary>
    [Fact]
    public async Task Cancel_PreservesHistoryAndExcludesFromTotals()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AccountingEntryService service = scope.ServiceProvider.GetRequiredService<AccountingEntryService>();
        AccountingEntryDto entry = await service.CreateManualAsync(
            new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), AccountingEntryType.Expense, "Limpieza", 1500m), clock.UtcNow);

        await service.CancelAsync(entry.Id, clock.UtcNow);

        AccountingEntryDto persisted = await service.GetByIdAsync(entry.Id);
        Assert.Equal(AccountingEntryStatus.Cancelled, persisted.Status);
        Assert.NotNull(persisted.CancelledAtUtc);

        AccountingSummaryDto summary = await service.GetSummaryAsync(
            new GetAccountingSummaryModel(null, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));
        Assert.Equal(0m, summary.TotalExpense);
        Assert.Equal(0m, summary.Balance);
    }

    /// <summary>Verifies that entry listing applies type, concept and range filters.</summary>
    [Fact]
    public async Task GetEntries_AppliesFilters()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AccountingEntryService service = scope.ServiceProvider.GetRequiredService<AccountingEntryService>();
        await service.CreateManualAsync(new CreateManualAccountingEntryModel(new DateOnly(2026, 9, 15), AccountingEntryType.Income, "Donación septiembre", 3000m), clock.UtcNow);
        await service.CreateManualAsync(new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Alquiler salón", 8000m), clock.UtcNow);
        await service.CreateManualAsync(new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 6), AccountingEntryType.Expense, "Alquiler insumos", 2000m), clock.UtcNow);

        IReadOnlyList<AccountingEntryDto> incomes = await service.GetEntriesAsync(new AccountingEntryFilter(null, null, AccountingEntryType.Income, null));
        Assert.Equal(2, incomes.Count);

        IReadOnlyList<AccountingEntryDto> byConcept = await service.GetEntriesAsync(new AccountingEntryFilter(null, null, null, "Alquiler"));
        Assert.Equal(2, byConcept.Count);

        IReadOnlyList<AccountingEntryDto> october = await service.GetEntriesAsync(
            new AccountingEntryFilter(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), null, null));
        Assert.Equal(2, october.Count);
        Assert.Equal(new DateOnly(2026, 10, 6), october[0].EntryDate);
    }

    /// <summary>Verifies that the summary aggregates active income and expense inside the range.</summary>
    [Fact]
    public async Task GetSummary_AggregatesActiveTotals()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        AccountingEntryService service = scope.ServiceProvider.GetRequiredService<AccountingEntryService>();
        await service.CreateManualAsync(new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 3000m), clock.UtcNow);
        await service.CreateManualAsync(new CreateManualAccountingEntryModel(new DateOnly(2026, 10, 6), AccountingEntryType.Expense, "Limpieza", 1000m), clock.UtcNow);
        await service.CreateManualAsync(new CreateManualAccountingEntryModel(new DateOnly(2026, 11, 1), AccountingEntryType.Income, "Fuera de rango", 5000m), clock.UtcNow);

        AccountingSummaryDto summary = await service.GetSummaryAsync(
            new GetAccountingSummaryModel(AccountingSummaryPeriod.CurrentMonth, null, null));

        Assert.Equal(new DateOnly(2026, 10, 1), summary.From);
        Assert.Equal(new DateOnly(2026, 10, 31), summary.To);
        Assert.Equal(3000m, summary.TotalIncome);
        Assert.Equal(1000m, summary.TotalExpense);
        Assert.Equal(2000m, summary.Balance);
    }
}

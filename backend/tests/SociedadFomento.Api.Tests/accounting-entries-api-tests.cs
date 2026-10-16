using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SociedadFomento.Api.Contracts.Accounting;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies the administrative accounting-entries HTTP contract.</summary>
public sealed class AccountingEntriesApiTests
{
    /// <summary>Verifies the manual entry lifecycle: create, list, filter, summarize and cancel.</summary>
    [Fact]
    public async Task AccountingEndpoints_AdministrativeLifecycle()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            HttpResponseMessage incomeResponse = await factory.Client.PostAsJsonAsync("/api/accounting-entries",
                new CreateManualAccountingEntryRequest(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 3000m));
            JsonDocument income = await JsonDocument.ParseAsync(await incomeResponse.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.Created, incomeResponse.StatusCode);
            long incomeId = income.RootElement.GetProperty("id").GetInt64();
            Assert.Equal("Income", income.RootElement.GetProperty("type").GetString());
            Assert.Equal("Manual", income.RootElement.GetProperty("source").GetString());

            await factory.Client.PostAsJsonAsync("/api/accounting-entries",
                new CreateManualAccountingEntryRequest(new DateOnly(2026, 10, 6), AccountingEntryType.Expense, "Limpieza", 1000m));
            await factory.Client.PostAsJsonAsync("/api/accounting-entries",
                new CreateManualAccountingEntryRequest(new DateOnly(2026, 9, 15), AccountingEntryType.Income, "Donación septiembre", 2000m));

            HttpResponseMessage list = await factory.Client.GetAsync("/api/accounting-entries");
            JsonDocument entries = await JsonDocument.ParseAsync(await list.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.Equal(3, entries.RootElement.GetArrayLength());

            HttpResponseMessage filtered = await factory.Client.GetAsync("/api/accounting-entries?type=Income&from=2026-10-01&to=2026-10-31");
            JsonDocument filteredEntries = await JsonDocument.ParseAsync(await filtered.Content.ReadAsStreamAsync());
            Assert.Single(filteredEntries.RootElement.EnumerateArray());

            HttpResponseMessage summaryResponse = await factory.Client.GetAsync("/api/accounting-entries/summary?period=CurrentMonth");
            JsonDocument summary = await JsonDocument.ParseAsync(await summaryResponse.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
            Assert.Equal(3000m, summary.RootElement.GetProperty("totalIncome").GetDecimal());
            Assert.Equal(1000m, summary.RootElement.GetProperty("totalExpense").GetDecimal());
            Assert.Equal(2000m, summary.RootElement.GetProperty("balance").GetDecimal());

            HttpResponseMessage cancel = await factory.Client.PostAsync($"/api/accounting-entries/{incomeId}/cancel", null);
            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

            HttpResponseMessage afterCancel = await factory.Client.GetAsync("/api/accounting-entries/summary?period=CurrentMonth");
            JsonDocument afterCancelSummary = await JsonDocument.ParseAsync(await afterCancel.Content.ReadAsStreamAsync());
            Assert.Equal(0m, afterCancelSummary.RootElement.GetProperty("totalIncome").GetDecimal());

            HttpResponseMessage recancel = await factory.Client.PostAsync($"/api/accounting-entries/{incomeId}/cancel", null);
            Assert.Equal(HttpStatusCode.Conflict, recancel.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that invalid payloads are rejected.</summary>
    [Fact]
    public async Task Create_RejectsInvalidInput()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            HttpResponseMessage zeroAmount = await factory.Client.PostAsJsonAsync("/api/accounting-entries",
                new CreateManualAccountingEntryRequest(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 0m));
            Assert.Equal(HttpStatusCode.BadRequest, zeroAmount.StatusCode);

            HttpResponseMessage emptyConcept = await factory.Client.PostAsJsonAsync("/api/accounting-entries",
                new CreateManualAccountingEntryRequest(new DateOnly(2026, 10, 5), AccountingEntryType.Income, " ", 1000m));
            Assert.Equal(HttpStatusCode.BadRequest, emptyConcept.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that anonymous requests are rejected.</summary>
    [Fact]
    public async Task AnonymousRequests_AreRejected()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        try
        {
            HttpResponseMessage response = await factory.Client.GetAsync("/api/accounting-entries");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that mutating endpoints require antiforgery tokens.</summary>
    [Fact]
    public async Task Create_RequiresAntiforgeryToken()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            factory.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
            HttpResponseMessage response = await factory.Client.PostAsJsonAsync("/api/accounting-entries",
                new CreateManualAccountingEntryRequest(new DateOnly(2026, 10, 5), AccountingEntryType.Income, "Donación", 3000m));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }
}

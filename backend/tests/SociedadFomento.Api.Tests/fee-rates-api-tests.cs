using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SociedadFomento.Api.Contracts.FeeRates;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies the administrative fee-rates HTTP contract.</summary>
public sealed class FeeRatesApiTests
{
    /// <summary>Verifies CRUD-like lifecycle and applicable-rate lookup.</summary>
    [Fact]
    public async Task FeeRateEndpoints_AdministrativeLifecycle()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            HttpResponseMessage created = await factory.Client.PostAsJsonAsync("/api/fee-rates", new CreateFeeRateRequest(new DateOnly(2026, 1, 1), 5000m));
            JsonDocument rate = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            long rateId = rate.RootElement.GetProperty("id").GetInt64();
            Assert.Equal(new DateOnly(2026, 1, 1).ToString("O"), rate.RootElement.GetProperty("effectiveMonth").GetString());
            Assert.Equal(5000m, rate.RootElement.GetProperty("amount").GetDecimal());

            await factory.Client.PostAsJsonAsync("/api/fee-rates", new CreateFeeRateRequest(new DateOnly(2026, 7, 1), 7000m));
            await factory.Client.PostAsJsonAsync("/api/fee-rates", new CreateFeeRateRequest(new DateOnly(2026, 10, 1), 9000m));

            HttpResponseMessage list = await factory.Client.GetAsync("/api/fee-rates");
            JsonDocument history = await JsonDocument.ParseAsync(await list.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.Equal(3, history.RootElement.GetArrayLength());

            HttpResponseMessage current = await factory.Client.GetAsync("/api/fee-rates/current");
            JsonDocument currentRate = await JsonDocument.ParseAsync(await current.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, current.StatusCode);
            Assert.Equal(9000m, currentRate.RootElement.GetProperty("amount").GetDecimal());

            HttpResponseMessage applicable = await factory.Client.GetAsync("/api/fee-rates/applicable?month=2026-09-01");
            JsonDocument applicableRate = await JsonDocument.ParseAsync(await applicable.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, applicable.StatusCode);
            Assert.Equal(7000m, applicableRate.RootElement.GetProperty("amount").GetDecimal());

            HttpResponseMessage byId = await factory.Client.GetAsync($"/api/fee-rates/{rateId}");
            Assert.Equal(HttpStatusCode.OK, byId.StatusCode);

            HttpResponseMessage duplicate = await factory.Client.PostAsJsonAsync("/api/fee-rates", new CreateFeeRateRequest(new DateOnly(2026, 1, 1), 9000m));
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

            HttpResponseMessage invalidAmount = await factory.Client.PostAsJsonAsync("/api/fee-rates", new CreateFeeRateRequest(new DateOnly(2026, 11, 1), 0m));
            Assert.Equal(HttpStatusCode.BadRequest, invalidAmount.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies anonymous requests are rejected.</summary>
    [Fact]
    public async Task AnonymousRequests_AreRejected()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        try
        {
            HttpResponseMessage response = await factory.Client.GetAsync("/api/fee-rates");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies mutating endpoints require antiforgery tokens.</summary>
    [Fact]
    public async Task Create_RequiresAntiforgeryToken()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            factory.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
            HttpResponseMessage response = await factory.Client.PostAsJsonAsync("/api/fee-rates", new CreateFeeRateRequest(new DateOnly(2026, 1, 1), 5000m));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }
}

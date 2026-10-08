using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SociedadFomento.Api.Contracts.FeeObligations;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies the administrative fee-obligation generation HTTP contract.</summary>
public sealed class FeeObligationsApiTests
{
    /// <summary>Verifies generation creates obligations for active members and is idempotent.</summary>
    [Fact]
    public async Task Generate_AdministrativeLifecycle()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            await factory.Client.PostAsJsonAsync("/api/fee-rates", new Contracts.FeeRates.CreateFeeRateRequest(new DateOnly(2026, 9, 1), 5000m));
            factory.Clock.SetUtcNow(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
            await factory.Client.PostAsJsonAsync("/api/members", CreateMemberRequest("12345678"));
            await factory.Client.PostAsJsonAsync("/api/members", CreateMemberRequest("87654321"));

            HttpResponseMessage response = await factory.Client.PostAsJsonAsync("/api/fee-obligations/generate", new GenerateFeeObligationsRequest(new DateOnly(2026, 10, 1)));
            JsonDocument result = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(2, result.RootElement.GetProperty("generatedCount").GetInt32());
            Assert.Equal("2026-10-01", result.RootElement.GetProperty("month").GetString());

            HttpResponseMessage second = await factory.Client.PostAsJsonAsync("/api/fee-obligations/generate", new GenerateFeeObligationsRequest(new DateOnly(2026, 10, 1)));
            JsonDocument secondResult = await JsonDocument.ParseAsync(await second.Content.ReadAsStreamAsync());
            Assert.Equal(0, secondResult.RootElement.GetProperty("generatedCount").GetInt32());
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that anonymous requests are rejected.</summary>
    [Fact]
    public async Task Generate_AnonymousRequestIsRejected()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        try
        {
            HttpResponseMessage response = await factory.Client.PostAsJsonAsync("/api/fee-obligations/generate", new GenerateFeeObligationsRequest(new DateOnly(2026, 10, 1)));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that mutating endpoint requires antiforgery token.</summary>
    [Fact]
    public async Task Generate_RequiresAntiforgeryToken()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            factory.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
            HttpResponseMessage response = await factory.Client.PostAsJsonAsync("/api/fee-obligations/generate", new GenerateFeeObligationsRequest(new DateOnly(2026, 10, 1)));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    private static Contracts.Members.CreateMemberRequest CreateMemberRequest(string dni) => new(
        dni, "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", "ana@example.com");
}

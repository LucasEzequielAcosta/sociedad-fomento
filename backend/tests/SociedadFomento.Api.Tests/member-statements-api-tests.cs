using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies the administrative member-statement HTTP contract.</summary>
public sealed class MemberStatementsApiTests
{
    /// <summary>Verifies that an admin can retrieve a member statement.</summary>
    [Fact]
    public async Task Get_AdministrativeLifecycle()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            await factory.Client.PostAsJsonAsync("/api/fee-rates", new Contracts.FeeRates.CreateFeeRateRequest(new DateOnly(2026, 9, 1), 7000m));
            factory.Clock.SetUtcNow(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
            HttpResponseMessage createdMember = await factory.Client.PostAsJsonAsync("/api/members", CreateMemberRequest("12345678"));
            JsonDocument member = await JsonDocument.ParseAsync(await createdMember.Content.ReadAsStreamAsync());
            long memberId = member.RootElement.GetProperty("id").GetInt64();
            factory.Clock.SetUtcNow(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));
            await factory.Client.PostAsJsonAsync("/api/fee-obligations/generate", new Contracts.FeeObligations.GenerateFeeObligationsRequest(new DateOnly(2026, 10, 1)));

            HttpResponseMessage response = await factory.Client.GetAsync($"/api/member-statements/{memberId}");
            JsonDocument statement = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(memberId, statement.RootElement.GetProperty("memberId").GetInt64());
            Assert.True(statement.RootElement.GetProperty("obligations").GetArrayLength() > 0);
            Assert.Equal(14000m, statement.RootElement.GetProperty("totalDebt").GetDecimal());
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that anonymous requests are rejected.</summary>
    [Fact]
    public async Task Get_AnonymousRequestIsRejected()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        try
        {
            HttpResponseMessage response = await factory.Client.GetAsync("/api/member-statements/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    private static Contracts.Members.CreateMemberRequest CreateMemberRequest(string dni) => new(
        dni, "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", "ana@example.com");
}

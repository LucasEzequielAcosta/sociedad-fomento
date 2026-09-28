using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SociedadFomento.Api.Contracts.Members;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies the administrative Members HTTP contract.</summary>
public sealed class MembersApiTests
{
    /// <summary>Verifies stable 409 and complete rollback when no fee rate exists.</summary>
    [Fact]
    public async Task CreateWithoutFeeRate_ReturnsConflictAndDoesNotPersistMember()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            HttpResponseMessage response = await factory.Client.PostAsJsonAsync("/api/members", CreateRequest("12345678"));
            JsonDocument problem = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("FEE_RATE_NOT_FOUND", problem.RootElement.GetProperty("code").GetString());
            HttpResponseMessage search = await factory.Client.GetAsync("/api/members");
            JsonDocument page = await JsonDocument.ParseAsync(await search.Content.ReadAsStreamAsync());
            Assert.Equal(0, page.RootElement.GetProperty("totalCount").GetInt32());
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies registration, normalized lookup, duplicate conflict and lifecycle endpoints.</summary>
    [Fact]
    public async Task MemberEndpoints_CompleteAdministrativeLifecycle()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            HttpResponseMessage created = await factory.Client.PostAsJsonAsync("/api/members", CreateRequest("12.345.678"));
            JsonDocument member = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
            long memberId = member.RootElement.GetProperty("id").GetInt64();
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);

            HttpResponseMessage byDni = await factory.Client.GetAsync("/api/members/by-dni/12345678");
            Assert.Equal(HttpStatusCode.OK, byDni.StatusCode);
            HttpResponseMessage search = await factory.Client.GetAsync("/api/members?name=Ana&lastName=P%C3%A9rez");
            JsonDocument searchPage = await JsonDocument.ParseAsync(await search.Content.ReadAsStreamAsync());
            Assert.Equal(1, searchPage.RootElement.GetProperty("totalCount").GetInt32());

            UpdateMemberPersonalDataRequest update = new(
                "Ana María", "Pérez", "Calle 2", new DateOnly(1990, 1, 1), "456", "ana.nueva@example.com");
            HttpResponseMessage updated = await factory.Client.PutAsJsonAsync($"/api/members/{memberId}/personal-data", update);
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

            HttpResponseMessage duplicate = await factory.Client.PostAsJsonAsync("/api/members", CreateRequest("12345678"));
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

            HttpResponseMessage deactivated = await factory.Client.PostAsync($"/api/members/{memberId}/deactivate", null);
            Assert.Equal(HttpStatusCode.NoContent, deactivated.StatusCode);

            HttpResponseMessage sameDay = await factory.Client.PostAsync($"/api/members/{memberId}/reactivate", null);
            Assert.Equal(HttpStatusCode.Conflict, sameDay.StatusCode);

            factory.Clock.SetUtcNow(factory.Clock.UtcNow.AddDays(1));
            HttpResponseMessage reactivated = await factory.Client.PostAsync($"/api/members/{memberId}/reactivate", null);
            JsonDocument result = await JsonDocument.ParseAsync(await reactivated.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
            Assert.False(result.RootElement.GetProperty("obligationCreated").GetBoolean());

            HttpResponseMessage history = await factory.Client.GetAsync($"/api/members/{memberId}/membership-periods");
            Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies request validation and missing resource responses.</summary>
    [Fact]
    public async Task InvalidAndMissingRequests_ReturnClearClientErrors()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            HttpResponseMessage invalidPage = await factory.Client.GetAsync("/api/members?page=0");
            HttpResponseMessage missing = await factory.Client.GetAsync("/api/members/999");
            CreateMemberRequest invalidBirthDate = CreateRequest("12345678") with { BirthDate = default };
            HttpResponseMessage invalidMember = await factory.Client.PostAsJsonAsync("/api/members", invalidBirthDate);
            CreateMemberRequest invalidModel = CreateRequest("12345678") with { FirstName = string.Empty };
            HttpResponseMessage automaticValidation = await factory.Client.PostAsJsonAsync("/api/members", invalidModel);
            JsonDocument problem = await JsonDocument.ParseAsync(await automaticValidation.Content.ReadAsStreamAsync());

            Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, invalidMember.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, automaticValidation.StatusCode);
            Assert.Equal("INVALID_REQUEST", problem.RootElement.GetProperty("code").GetString());
            Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("FirstName", out _));
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    private static CreateMemberRequest CreateRequest(string dni) => new(
        dni, "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", "ana@example.com");
}

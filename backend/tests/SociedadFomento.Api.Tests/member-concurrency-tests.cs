using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Api.Contracts.Members;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies that concurrent member operations return stable conflicts without partial data.</summary>
public sealed class MemberConcurrencyTests
{
    /// <summary>Verifies that concurrent normalized DNI registration creates exactly one member.</summary>
    [Fact]
    public async Task ConcurrentCreate_SucceedsOnceAndReturnsOneConflict()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            Task<HttpResponseMessage> firstRequest = factory.Client.PostAsJsonAsync("/api/members", CreateRequest("12.345.678"));
            Task<HttpResponseMessage> secondRequest = factory.Client.PostAsJsonAsync("/api/members", CreateRequest("12345678"));
            HttpResponseMessage[] responses = await Task.WhenAll(firstRequest, secondRequest);

            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            HttpResponseMessage conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal("DNI_DUPLICATE", await ReadCodeAsync(conflict));
            await AssertPersistedCountsAsync(factory, expectedMembers: 1, expectedPeriods: 1, expectedObligations: 1);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that concurrent reactivation creates one open period and returns one stable conflict.</summary>
    [Fact]
    public async Task ConcurrentReactivate_SucceedsOnceAndReturnsMemberStateConflict()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            HttpResponseMessage created = await factory.Client.PostAsJsonAsync("/api/members", CreateRequest("12345678"));
            JsonDocument member = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
            long memberId = member.RootElement.GetProperty("id").GetInt64();
            await factory.Client.PostAsync($"/api/members/{memberId}/deactivate", null);
            factory.Clock.SetUtcNow(factory.Clock.UtcNow.AddDays(1));

            Task<HttpResponseMessage> firstRequest = factory.Client.PostAsync($"/api/members/{memberId}/reactivate", null);
            Task<HttpResponseMessage> secondRequest = factory.Client.PostAsync($"/api/members/{memberId}/reactivate", null);
            HttpResponseMessage[] responses = await Task.WhenAll(firstRequest, secondRequest);

            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            HttpResponseMessage conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal("MEMBER_STATE_CONFLICT", await ReadCodeAsync(conflict));
            await AssertPersistedCountsAsync(factory, expectedMembers: 1, expectedPeriods: 2, expectedObligations: 1);
            await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
            SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
            Assert.Equal(1, await context.MembershipPeriods.CountAsync(period => period.EndDate == null));
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response)
    {
        JsonDocument problem = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return problem.RootElement.GetProperty("code").GetString();
    }

    private static async Task AssertPersistedCountsAsync(
        MemberApiFactory factory, int expectedMembers, int expectedPeriods, int expectedObligations)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        Assert.Equal(expectedMembers, await context.Members.CountAsync());
        Assert.Equal(expectedPeriods, await context.MembershipPeriods.CountAsync());
        Assert.Equal(expectedObligations, await context.FeeObligations.CountAsync());
    }

    private static CreateMemberRequest CreateRequest(string dni) => new(
        dni, "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", "ana@example.com");
}

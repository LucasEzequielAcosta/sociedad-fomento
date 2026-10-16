using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Api.Contracts.MemberPortal;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies the member self-service portal HTTP contract.</summary>
public sealed class MemberPortalApiTests
{
    /// <summary>Verifies that a member logs in with its DNI and reads only its own statement.</summary>
    [Fact]
    public async Task MemberLogin_AllowsAccessToOwnStatement()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await SeedMembersAsync(factory);
        await factory.AddAntiforgeryTokenAsync();
        try
        {
            HttpResponseMessage login = await factory.Client.PostAsJsonAsync("/api/portal/login", new MemberLoginRequest("11.111.111"));
            Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);

            HttpResponseMessage me = await factory.Client.GetAsync("/api/portal/me");
            JsonDocument meBody = await JsonDocument.ParseAsync(await me.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            Assert.Equal("11111111", meBody.RootElement.GetProperty("dni").GetString());
            Assert.Equal("Ana", meBody.RootElement.GetProperty("firstName").GetString());

            HttpResponseMessage statement = await factory.Client.GetAsync("/api/portal/statement");
            JsonDocument statementBody = await JsonDocument.ParseAsync(await statement.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, statement.StatusCode);
            Assert.Equal("11111111", statementBody.RootElement.GetProperty("dni").GetString());
            Assert.Equal(7000m, statementBody.RootElement.GetProperty("totalDebt").GetDecimal());

            HttpResponseMessage adminEndpoint = await factory.Client.GetAsync("/api/members");
            Assert.Equal(HttpStatusCode.Unauthorized, adminEndpoint.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that an unknown DNI is rejected.</summary>
    [Fact]
    public async Task MemberLogin_UnknownDniIsRejected()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AddAntiforgeryTokenAsync();
        try
        {
            HttpResponseMessage login = await factory.Client.PostAsJsonAsync("/api/portal/login", new MemberLoginRequest("99999999"));
            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that anonymous requests cannot read portal data.</summary>
    [Fact]
    public async Task AnonymousRequests_AreRejected()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        try
        {
            HttpResponseMessage response = await factory.Client.GetAsync("/api/portal/statement");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that a member session cannot read another member's data.</summary>
    [Fact]
    public async Task MemberStatement_IsScopedToAuthenticatedMember()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await SeedMembersAsync(factory);
        await factory.AddAntiforgeryTokenAsync();
        try
        {
            await factory.Client.PostAsJsonAsync("/api/portal/login", new MemberLoginRequest("22222222"));

            HttpResponseMessage statement = await factory.Client.GetAsync("/api/portal/statement");
            JsonDocument body = await JsonDocument.ParseAsync(await statement.Content.ReadAsStreamAsync());

            Assert.Equal("22222222", body.RootElement.GetProperty("dni").GetString());
            Assert.Equal("Bruno", body.RootElement.GetProperty("firstName").GetString());
            Assert.Equal(0m, body.RootElement.GetProperty("totalDebt").GetDecimal());
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    private static async Task SeedMembersAsync(MemberApiFactory factory)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        Member ana = new("11111111", "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null, new DateOnly(2026, 9, 1));
        Member bruno = new("22222222", "Bruno", "López", "Calle 2", new DateOnly(1985, 5, 5), "456", null, new DateOnly(2026, 9, 1));
        FeeRate rate = new(new DateOnly(2026, 9, 1), 7000m, factory.Clock.UtcNow);
        context.Members.AddRange(ana, bruno);
        context.FeeRates.Add(rate);
        context.FeeObligations.Add(new FeeObligation(ana, ana.MembershipPeriods.Single(), rate, new DateOnly(2026, 9, 1), factory.Clock.UtcNow));
        await context.SaveChangesAsync();
    }
}

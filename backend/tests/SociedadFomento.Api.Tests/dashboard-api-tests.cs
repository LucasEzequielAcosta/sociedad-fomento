using System.Net;
using System.Text.Json;
using SociedadFomento.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies the administrative dashboard HTTP contract.</summary>
public sealed class DashboardApiTests
{
    /// <summary>Verifies that the dashboard returns member, obligation and economy sections.</summary>
    [Fact]
    public async Task Get_ReturnsDashboardSummary()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        await SeedDataAsync(factory);
        try
        {
            HttpResponseMessage response = await factory.Client.GetAsync("/api/dashboard");
            JsonDocument dashboard = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, dashboard.RootElement.GetProperty("members").GetProperty("active").GetInt32());
            Assert.Equal(0, dashboard.RootElement.GetProperty("members").GetProperty("inactive").GetInt32());
            Assert.Equal(1, dashboard.RootElement.GetProperty("obligations").GetProperty("pending").GetInt32());
            Assert.Equal(7000m, dashboard.RootElement.GetProperty("obligations").GetProperty("totalDebt").GetDecimal());
            Assert.Equal(0m, dashboard.RootElement.GetProperty("economy").GetProperty("totalIncome").GetDecimal());
            Assert.Equal(0m, dashboard.RootElement.GetProperty("economy").GetProperty("balance").GetDecimal());
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
            HttpResponseMessage response = await factory.Client.GetAsync("/api/dashboard");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    private static async Task SeedDataAsync(MemberApiFactory factory)
    {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        Member member = new("11111111", "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null, new DateOnly(2026, 10, 1));
        FeeRate rate = new(new DateOnly(2026, 10, 1), 7000m, factory.Clock.UtcNow);
        context.Members.Add(member);
        context.FeeRates.Add(rate);
        context.FeeObligations.Add(new FeeObligation(member, member.MembershipPeriods.Single(), rate, new DateOnly(2026, 10, 1), factory.Clock.UtcNow));
        await context.SaveChangesAsync();
    }
}

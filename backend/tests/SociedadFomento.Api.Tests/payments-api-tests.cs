using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Api.Contracts.Payments;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies the administrative payments HTTP contract.</summary>
public sealed class PaymentsApiTests
{
    /// <summary>Verifies payment registration and automatic accounting entry creation.</summary>
    [Fact]
    public async Task Create_RegistersPaymentAndCreatesAccountingEntry()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            await factory.Client.PostAsJsonAsync("/api/fee-rates", new Contracts.FeeRates.CreateFeeRateRequest(new DateOnly(2026, 9, 1), 5000m));
            factory.Clock.SetUtcNow(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
            HttpResponseMessage createdMember = await factory.Client.PostAsJsonAsync("/api/members", CreateMemberRequest("12345678"));
            JsonDocument member = await JsonDocument.ParseAsync(await createdMember.Content.ReadAsStreamAsync());
            long memberId = member.RootElement.GetProperty("id").GetInt64();
            factory.Clock.SetUtcNow(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));
            await factory.Client.PostAsJsonAsync("/api/fee-obligations/generate", new Contracts.FeeObligations.GenerateFeeObligationsRequest(new DateOnly(2026, 10, 1)));
            await using AsyncServiceScope dbScope = factory.Services.CreateAsyncScope();
            SociedadFomentoDbContext context = dbScope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
            long obligationId = await context.FeeObligations.Where(item => item.MemberId == memberId && item.Period == new DateOnly(2026, 10, 1))
                .Select(item => item.Id).SingleAsync();

            HttpResponseMessage response = await factory.Client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest(
                memberId, new DateOnly(2026, 10, 15), 5000m, PaymentMethod.Cash, [obligationId]));
            JsonDocument payment = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(memberId, payment.RootElement.GetProperty("memberId").GetInt64());
            Assert.Equal(5000m, payment.RootElement.GetProperty("amount").GetDecimal());
            Assert.Equal("Cash", payment.RootElement.GetProperty("method").GetString());
            Assert.Equal("Active", payment.RootElement.GetProperty("status").GetString());
            Assert.Equal(1, payment.RootElement.GetProperty("allocations").GetArrayLength());
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that anonymous requests are rejected.</summary>
    [Fact]
    public async Task Create_AnonymousRequestIsRejected()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        try
        {
            HttpResponseMessage response = await factory.Client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest(
                1, new DateOnly(2026, 10, 15), 5000m, PaymentMethod.Cash, [1]));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    /// <summary>Verifies that mutating endpoint requires antiforgery token.</summary>
    [Fact]
    public async Task Create_RequiresAntiforgeryToken()
    {
        MemberApiFactory factory = new(withFeeRate: true);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            factory.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
            HttpResponseMessage response = await factory.Client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest(
                1, new DateOnly(2026, 10, 15), 5000m, PaymentMethod.Cash, [1]));
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

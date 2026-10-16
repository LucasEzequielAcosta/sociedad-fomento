using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Api.Contracts.Payments;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Api.Tests;

/// <summary>Verifies payments listing, cancellation and CSV export endpoints.</summary>
public sealed class ExportsApiTests
{
    /// <summary>Verifies payments listing, cancellation and CSV exports across resources.</summary>
    [Fact]
    public async Task PaymentsListCancelAndExports_AdministrativeLifecycle()
    {
        MemberApiFactory factory = new(withFeeRate: false);
        await factory.InitializeDatabaseAsync();
        await factory.AuthenticateAsync();
        try
        {
            long obligationId = await CreatePaidObligationScenarioAsync(factory);

            HttpResponseMessage list = await factory.Client.GetAsync("/api/payments");
            JsonDocument payments = await JsonDocument.ParseAsync(await list.Content.ReadAsStreamAsync());
            Assert.Equal(HttpStatusCode.OK, list.StatusCode);
            Assert.Single(payments.RootElement.EnumerateArray());
            long paymentId = payments.RootElement[0].GetProperty("id").GetInt64();

            HttpResponseMessage filtered = await factory.Client.GetAsync($"/api/payments?from=2026-09-01&to=2026-09-30&method=Cash");
            JsonDocument filteredPayments = await JsonDocument.ParseAsync(await filtered.Content.ReadAsStreamAsync());
            Assert.Single(filteredPayments.RootElement.EnumerateArray());

            HttpResponseMessage paymentsCsv = await factory.Client.GetAsync("/api/payments/export");
            string paymentsCsvContent = Encoding.UTF8.GetString(await paymentsCsv.Content.ReadAsByteArrayAsync());
            Assert.Equal(HttpStatusCode.OK, paymentsCsv.StatusCode);
            Assert.Equal("text/csv", paymentsCsv.Content.Headers.ContentType?.MediaType);
            Assert.Contains("12345678", paymentsCsvContent);
            Assert.Contains("2026-09", paymentsCsvContent);

            HttpResponseMessage membersCsv = await factory.Client.GetAsync("/api/members/export");
            string membersCsvContent = Encoding.UTF8.GetString(await membersCsv.Content.ReadAsByteArrayAsync());
            Assert.Equal(HttpStatusCode.OK, membersCsv.StatusCode);
            Assert.Contains("Dni", membersCsvContent);
            Assert.Contains("Ana", membersCsvContent);

            HttpResponseMessage entriesCsv = await factory.Client.GetAsync("/api/accounting-entries/export");
            string entriesCsvContent = Encoding.UTF8.GetString(await entriesCsv.Content.ReadAsByteArrayAsync());
            Assert.Equal(HttpStatusCode.OK, entriesCsv.StatusCode);
            Assert.Contains("FeePayment", entriesCsvContent);

            HttpResponseMessage cancel = await factory.Client.PostAsync($"/api/payments/{paymentId}/cancel", null);
            Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

            HttpResponseMessage entriesAfterCancel = await factory.Client.GetAsync("/api/accounting-entries");
            JsonDocument entries = await JsonDocument.ParseAsync(await entriesAfterCancel.Content.ReadAsStreamAsync());
            Assert.Equal("Cancelled", entries.RootElement[0].GetProperty("status").GetString());

            HttpResponseMessage missing = await factory.Client.PostAsync("/api/payments/999999/cancel", null);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
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
            Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Client.GetAsync("/api/payments")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Client.GetAsync("/api/members/export")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await factory.Client.GetAsync("/api/accounting-entries/export")).StatusCode);
        }
        finally
        {
            await factory.DeleteDatabaseAsync();
        }
    }

    private static async Task<long> CreatePaidObligationScenarioAsync(MemberApiFactory factory)
    {
        await factory.Client.PostAsJsonAsync("/api/fee-rates", new Contracts.FeeRates.CreateFeeRateRequest(new DateOnly(2026, 9, 1), 5000m));
        factory.Clock.SetUtcNow(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        HttpResponseMessage createdMember = await factory.Client.PostAsJsonAsync("/api/members", new Contracts.Members.CreateMemberRequest(
            "12345678", "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null));
        JsonDocument member = await JsonDocument.ParseAsync(await createdMember.Content.ReadAsStreamAsync());
        long memberId = member.RootElement.GetProperty("id").GetInt64();

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        long obligationId = await context.FeeObligations.Where(item => item.MemberId == memberId)
            .Select(item => item.Id).SingleAsync();

        HttpResponseMessage payment = await factory.Client.PostAsJsonAsync("/api/payments",
            new CreatePaymentRequest(memberId, new DateOnly(2026, 9, 15), 5000m, PaymentMethod.Cash, [obligationId]));
        Assert.Equal(HttpStatusCode.OK, payment.StatusCode);
        return obligationId;
    }
}

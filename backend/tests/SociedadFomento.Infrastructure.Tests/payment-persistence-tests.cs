using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.FeeGeneration;
using SociedadFomento.Application.FeeGeneration.Models;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.Members.Dto;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Application.Payments;
using SociedadFomento.Application.Payments.Dto;
using SociedadFomento.Application.Payments.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies payment registration against an isolated SQL Server database.</summary>
public sealed class PaymentPersistenceTests : IAsyncLifetime
{
    private readonly string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoPaymentTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
    private readonly FixedClock clock = new(new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc));
    private ServiceProvider services = null!;

    /// <summary>Creates and migrates the isolated database.</summary>
    public async Task InitializeAsync()
    {
        ServiceCollection serviceCollection = new();
        serviceCollection.AddInfrastructure(connectionString);
        serviceCollection.AddSingleton<SociedadFomento.Application.Abstractions.IClock>(clock);
        services = serviceCollection.BuildServiceProvider();
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.MigrateAsync();
    }

    /// <summary>Deletes the isolated database.</summary>
    public async Task DisposeAsync()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await context.Database.EnsureDeletedAsync();
        await services.DisposeAsync();
    }

    /// <summary>Verifies that a payment is registered, allocated and creates an automatic accounting entry.</summary>
    [Fact]
    public async Task Register_CreatesPaymentAllocationsAndAccountingEntry()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        PaymentService paymentService = scope.ServiceProvider.GetRequiredService<PaymentService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        MemberDetailDto member = await CreateMemberWithObligationsAsync(scope);
        List<FeeObligation> obligations = await context.FeeObligations.Where(item => item.MemberId == member.Id).ToListAsync();

        PaymentDto result = await paymentService.RegisterAsync(new CreatePaymentModel(
            member.Id, new DateOnly(2026, 10, 15), obligations.Sum(item => item.Amount), PaymentMethod.BankTransfer,
            obligations.Select(item => item.Id).ToList()), clock.UtcNow);

        Assert.Equal(obligations.Count, result.Allocations.Count);
        Assert.Equal(1, await context.Payments.CountAsync());
        Assert.Equal(obligations.Count, await context.PaymentAllocations.CountAsync());
        AccountingEntry entry = await context.AccountingEntries.SingleAsync();
        Assert.Equal(result.Amount, entry.Amount);
        Assert.Equal(AccountingEntryType.Income, entry.Type);
        Assert.Equal(result.PaymentDate, entry.EntryDate);
        Assert.Equal(AccountingEntrySource.FeePayment, entry.Source);
    }

    /// <summary>Verifies that a partial payment is rejected.</summary>
    [Fact]
    public async Task Register_RejectsPartialPayment()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        PaymentService paymentService = scope.ServiceProvider.GetRequiredService<PaymentService>();
        MemberDetailDto member = await CreateMemberWithObligationsAsync(scope);
        long obligationId = await scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>().FeeObligations
            .Where(item => item.MemberId == member.Id).Select(item => item.Id).FirstAsync();

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => paymentService.RegisterAsync(new CreatePaymentModel(
                member.Id, new DateOnly(2026, 10, 15), 1m, PaymentMethod.Cash, [obligationId]), clock.UtcNow));

        Assert.Equal("INVALID_PAYMENT", exception.Code);
    }

    /// <summary>Verifies that paying an already paid obligation is rejected.</summary>
    [Fact]
    public async Task Register_RejectsAlreadyPaidObligation()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        PaymentService paymentService = scope.ServiceProvider.GetRequiredService<PaymentService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        MemberDetailDto member = await CreateMemberWithObligationsAsync(scope);
        List<FeeObligation> obligations = await context.FeeObligations.Where(item => item.MemberId == member.Id).ToListAsync();
        await paymentService.RegisterAsync(new CreatePaymentModel(
            member.Id, new DateOnly(2026, 10, 15), obligations.Sum(item => item.Amount), PaymentMethod.Cash,
            obligations.Select(item => item.Id).ToList()), clock.UtcNow);
        context.ChangeTracker.Clear();

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => paymentService.RegisterAsync(new CreatePaymentModel(
                member.Id, new DateOnly(2026, 10, 16), obligations.Sum(item => item.Amount), PaymentMethod.Cash,
                obligations.Select(item => item.Id).ToList()), clock.UtcNow));

        Assert.Equal("OBLIGATION_ALREADY_PAID", exception.Code);
    }

    private async Task<MemberDetailDto> CreateMemberWithObligationsAsync(AsyncServiceScope scope)
    {
        MemberService memberService = scope.ServiceProvider.GetRequiredService<MemberService>();
        FeeGenerationService feeGenerationService = scope.ServiceProvider.GetRequiredService<FeeGenerationService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await CreateFeeRateAsync(context, new DateOnly(2026, 9, 1), 5000m);
        clock.SetUtcNow(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        MemberDetailDto member = await memberService.CreateAsync(new CreateMemberModel("12345678",
            new MemberPersonalData("Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null)));
        clock.SetUtcNow(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));
        await feeGenerationService.GenerateAsync(new GenerateFeeObligationsModel(new DateOnly(2026, 10, 1)), clock.UtcNow);
        return member;
    }

    private static async Task CreateFeeRateAsync(SociedadFomentoDbContext context, DateOnly effectiveMonth, decimal amount)
    {
        context.FeeRates.Add(new FeeRate(effectiveMonth, amount, DateTime.UtcNow));
        await context.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.FeeGeneration;
using SociedadFomento.Application.FeeGeneration.Models;
using SociedadFomento.Application.MemberStatements;
using SociedadFomento.Application.MemberStatements.Dto;
using SociedadFomento.Application.MemberStatements.Models;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.Members.Dto;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Application.Payments;
using SociedadFomento.Application.Payments.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies member account statements against an isolated SQL Server database.</summary>
public sealed class MemberStatementPersistenceTests : IAsyncLifetime
{
    private readonly string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoMemberStatementTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
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

    /// <summary>Verifies debt reflects pending/overdue obligations and excludes future ones.</summary>
    [Fact]
    public async Task Get_DebtExcludesFutureAndPaidObligations()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MemberStatementService statementService = scope.ServiceProvider.GetRequiredService<MemberStatementService>();
        MemberDetailDto member = await CreateMemberWithObligationsAsync(scope);

        MemberStatementDto result = await statementService.GetAsync(new GetMemberStatementModel(member.Id, new DateOnly(2026, 10, 15)));

        Assert.Equal(member.Id, result.MemberId);
        Assert.Equal(2, result.Obligations.Count);
        Assert.Equal(14000m, result.TotalDebt);
        Assert.Equal(FeeObligationStatus.Overdue, result.Obligations.Single(o => o.Period == new DateOnly(2026, 9, 1)).Status);
        Assert.Equal(FeeObligationStatus.Pending, result.Obligations.Single(o => o.Period == new DateOnly(2026, 10, 1)).Status);
    }

    /// <summary>Verifies that after paying all exigible obligations the debt is zero.</summary>
    [Fact]
    public async Task Get_AfterFullPaymentDebtIsZero()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MemberStatementService statementService = scope.ServiceProvider.GetRequiredService<MemberStatementService>();
        PaymentService paymentService = scope.ServiceProvider.GetRequiredService<PaymentService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        MemberDetailDto member = await CreateMemberWithObligationsAsync(scope);
        List<FeeObligation> obligations = await context.FeeObligations.Where(o => o.MemberId == member.Id).ToListAsync();
        await paymentService.RegisterAsync(new CreatePaymentModel(member.Id, new DateOnly(2026, 10, 15), 14000m, PaymentMethod.Cash, obligations.Select(o => o.Id).ToList()), clock.UtcNow);

        MemberStatementDto result = await statementService.GetAsync(new GetMemberStatementModel(member.Id, new DateOnly(2026, 10, 15)));

        Assert.Equal(0m, result.TotalDebt);
        Assert.Equal(14000m, result.TotalPaid);
        Assert.All(result.Obligations, obligation => Assert.Equal(FeeObligationStatus.Paid, obligation.Status));
    }

    private async Task<MemberDetailDto> CreateMemberWithObligationsAsync(AsyncServiceScope scope)
    {
        MemberService memberService = scope.ServiceProvider.GetRequiredService<MemberService>();
        FeeGenerationService feeGenerationService = scope.ServiceProvider.GetRequiredService<FeeGenerationService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        await CreateFeeRateAsync(context, new DateOnly(2026, 9, 1), 7000m);
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

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.FeeGeneration;
using SociedadFomento.Application.FeeGeneration.Dto;
using SociedadFomento.Application.FeeGeneration.Models;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.Members.Dto;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies fee-obligation generation against an isolated SQL Server database.</summary>
public sealed class FeeGenerationPersistenceTests : IAsyncLifetime
{
    private readonly string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoFeeGenerationTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
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

    /// <summary>Verifies that generation creates obligations for active members and skips inactive members.</summary>
    [Fact]
    public async Task Generate_CreatesForActiveAndSkipsInactive()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MemberService memberService = scope.ServiceProvider.GetRequiredService<MemberService>();
        FeeGenerationService feeGenerationService = scope.ServiceProvider.GetRequiredService<FeeGenerationService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        clock.SetUtcNow(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        await CreateFeeRateAsync(context, new DateOnly(2026, 9, 1), 5000m);
        MemberDetailDto active = await memberService.CreateAsync(new CreateMemberModel("12345678",
            new MemberPersonalData("Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null)));
        MemberDetailDto inactive = await memberService.CreateAsync(new CreateMemberModel("87654321",
            new MemberPersonalData("Luis", "Gómez", "Calle 2", new DateOnly(1990, 1, 1), "456", null)));
        await memberService.DeactivateAsync(inactive.Id);
        clock.SetUtcNow(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

        FeeGenerationResult result = await feeGenerationService.GenerateAsync(
            new GenerateFeeObligationsModel(new DateOnly(2026, 10, 1)), clock.UtcNow);

        Assert.Equal(1, result.GeneratedCount);
        Assert.Equal(2, await context.Members.CountAsync());
        Assert.Equal(3, await context.FeeObligations.CountAsync());
        FeeObligation obligation = await context.FeeObligations.SingleAsync(item => item.Period == new DateOnly(2026, 10, 1));
        Assert.Equal(active.Id, obligation.MemberId);
        Assert.Equal(5000m, obligation.Amount);
    }

    /// <summary>Verifies that generation is idempotent and does not duplicate obligations.</summary>
    [Fact]
    public async Task Generate_IsIdempotent()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MemberService memberService = scope.ServiceProvider.GetRequiredService<MemberService>();
        FeeGenerationService feeGenerationService = scope.ServiceProvider.GetRequiredService<FeeGenerationService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        clock.SetUtcNow(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        await CreateFeeRateAsync(context, new DateOnly(2026, 9, 1), 5000m);
        await memberService.CreateAsync(new CreateMemberModel("12345678",
            new MemberPersonalData("Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null)));
        clock.SetUtcNow(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

        await feeGenerationService.GenerateAsync(new GenerateFeeObligationsModel(new DateOnly(2026, 10, 1)), clock.UtcNow);
        FeeGenerationResult second = await feeGenerationService.GenerateAsync(new GenerateFeeObligationsModel(new DateOnly(2026, 10, 1)), clock.UtcNow);

        Assert.Equal(0, second.GeneratedCount);
        Assert.Equal(2, await context.FeeObligations.CountAsync());
    }

    /// <summary>Verifies that generation fails when no fee rate exists for the target month.</summary>
    [Fact]
    public async Task Generate_WithoutFeeRateThrowsBusinessRuleException()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MemberService memberService = scope.ServiceProvider.GetRequiredService<MemberService>();
        FeeGenerationService feeGenerationService = scope.ServiceProvider.GetRequiredService<FeeGenerationService>();
        clock.SetUtcNow(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        await CreateFeeRateAsync(scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>(), new DateOnly(2026, 9, 1), 5000m);
        await memberService.CreateAsync(new CreateMemberModel("12345678",
            new MemberPersonalData("Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null)));

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => feeGenerationService.GenerateAsync(new GenerateFeeObligationsModel(new DateOnly(2026, 8, 1)), clock.UtcNow));

        Assert.Equal("FEE_RATE_NOT_FOUND", exception.Code);
    }

    /// <summary>Verifies that reactivating a member in a new month allows generating a new obligation.</summary>
    [Fact]
    public async Task Generate_AfterReactivationCreatesNewObligationForNewPeriod()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MemberService memberService = scope.ServiceProvider.GetRequiredService<MemberService>();
        FeeGenerationService feeGenerationService = scope.ServiceProvider.GetRequiredService<FeeGenerationService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        clock.SetUtcNow(new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc));
        await CreateFeeRateAsync(context, new DateOnly(2026, 9, 1), 5000m);
        MemberDetailDto member = await memberService.CreateAsync(new CreateMemberModel("12345678",
            new MemberPersonalData("Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null)));
        await memberService.DeactivateAsync(member.Id);
        clock.SetUtcNow(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
        await memberService.ReactivateAsync(member.Id);
        await CreateFeeRateAsync(context, new DateOnly(2026, 11, 1), 7000m);
        clock.SetUtcNow(new DateTime(2026, 12, 1, 12, 0, 0, DateTimeKind.Utc));

        FeeGenerationResult result = await feeGenerationService.GenerateAsync(
            new GenerateFeeObligationsModel(new DateOnly(2026, 12, 1)), clock.UtcNow);

        Assert.Equal(1, result.GeneratedCount);
        FeeObligation obligation = await context.FeeObligations
            .Where(item => item.Period == new DateOnly(2026, 12, 1)).SingleAsync();
        Assert.Equal(7000m, obligation.Amount);
    }

    private static async Task CreateFeeRateAsync(SociedadFomentoDbContext context, DateOnly effectiveMonth, decimal amount)
    {
        context.FeeRates.Add(new FeeRate(effectiveMonth, amount, DateTime.UtcNow));
        await context.SaveChangesAsync();
    }
}

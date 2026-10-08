using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.FeeRates;
using SociedadFomento.Application.FeeRates.Dto;
using SociedadFomento.Application.FeeRates.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies fee-rate persistence against an isolated SQL Server database.</summary>
public sealed class FeeRatePersistenceTests : IAsyncLifetime
{
    private readonly string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoFeeRateTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
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

    /// <summary>Verifies that a created fee rate is persisted with normalized month.</summary>
    [Fact]
    public async Task Create_PersistsNormalizedFeeRate()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        FeeRateService service = scope.ServiceProvider.GetRequiredService<FeeRateService>();

        FeeRateDto result = await service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 7, 15), 7000m), clock.UtcNow);

        Assert.Equal(new DateOnly(2026, 7, 1), result.EffectiveMonth);
        Assert.Equal(7000m, result.Amount);
    }

    /// <summary>Verifies that duplicate effective months are rejected at the database level.</summary>
    [Fact]
    public async Task Create_DuplicateMonthThrowsBusinessRuleException()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        FeeRateService service = scope.ServiceProvider.GetRequiredService<FeeRateService>();
        await service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 7, 1), 7000m), clock.UtcNow);

        BusinessRuleException exception = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 7, 1), 9000m), clock.UtcNow));

        Assert.Equal("FEE_RATE_MONTH_DUPLICATE", exception.Code);
    }

    /// <summary>Verifies that the history is returned ordered by effective month.</summary>
    [Fact]
    public async Task GetAll_ReturnsOrderedHistory()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        FeeRateService service = scope.ServiceProvider.GetRequiredService<FeeRateService>();
        await service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 10, 1), 9000m), clock.UtcNow);
        await service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 1, 1), 5000m), clock.UtcNow);
        await service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 7, 1), 7000m), clock.UtcNow);

        IReadOnlyList<FeeRateDto> result = await service.GetAllAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), result[0].EffectiveMonth);
        Assert.Equal(new DateOnly(2026, 7, 1), result[1].EffectiveMonth);
        Assert.Equal(new DateOnly(2026, 10, 1), result[2].EffectiveMonth);
    }

    /// <summary>Verifies that the applicable rate is resolved from the most recent earlier or equal month.</summary>
    [Fact]
    public async Task GetApplicable_ReturnsMostRecentMatchingRate()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        FeeRateService service = scope.ServiceProvider.GetRequiredService<FeeRateService>();
        await service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 1, 1), 5000m), clock.UtcNow);
        await service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 7, 1), 7000m), clock.UtcNow);

        FeeRateDto result = await service.GetApplicableAsync(new DateOnly(2026, 9, 1));

        Assert.Equal(7000m, result.Amount);
    }

    /// <summary>Verifies that a fee rate used by an obligation cannot be deleted.</summary>
    [Fact]
    public async Task UsedFeeRate_CannotBeDeleted()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        FeeRateService service = scope.ServiceProvider.GetRequiredService<FeeRateService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        FeeRateDto rate = await service.CreateAsync(new CreateFeeRateModel(new DateOnly(2026, 9, 1), 5000m), clock.UtcNow);
        Member member = new("12345678", "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null, new DateOnly(2026, 9, 1));
        FeeRate feeRate = await context.FeeRates.SingleAsync(item => item.Id == rate.Id);
        context.FeeObligations.Add(new FeeObligation(member, member.MembershipPeriods.Single(), feeRate, new DateOnly(2026, 9, 1), clock.UtcNow));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        SqlException exception = await Assert.ThrowsAsync<SqlException>(
            () => context.FeeRates.Where(item => item.Id == rate.Id).ExecuteDeleteAsync());

        Assert.Equal(547, exception.Number);
    }
}

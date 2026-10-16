using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Payments;
using SociedadFomento.Application.Payments.Dto;
using SociedadFomento.Application.Payments.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies payment listing queries against an isolated SQL Server database.</summary>
public sealed class PaymentListPersistenceTests : IAsyncLifetime
{
    private readonly string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database=SociedadFomentoPaymentListTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
    private readonly FixedClock clock = new(new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc));
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

    /// <summary>Verifies that member, range and method filters return the expected payments.</summary>
    [Fact]
    public async Task GetPayments_AppliesFilters()
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        PaymentService service = scope.ServiceProvider.GetRequiredService<PaymentService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        Payment anaCash = await PersistenceTestData.AddPaymentAsync(context, "11111111", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 15));
        await PersistenceTestData.AddPaymentAsync(context, "22222222", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5));

        IReadOnlyList<PaymentListItemDto> all = await service.GetPaymentsAsync(new PaymentFilter(null, null, null, null));
        Assert.Equal(2, all.Count);
        Assert.Equal("22222222", all[0].MemberDni);

        IReadOnlyList<PaymentListItemDto> byMember = await service.GetPaymentsAsync(new PaymentFilter(anaCash.MemberId, null, null, null));
        PaymentListItemDto ana = Assert.Single(byMember);
        Assert.Equal("11111111", ana.MemberDni);
        Assert.Equal([new DateOnly(2026, 9, 1)], ana.Periods);

        IReadOnlyList<PaymentListItemDto> october = await service.GetPaymentsAsync(
            new PaymentFilter(null, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), null));
        Assert.Single(october);

        IReadOnlyList<PaymentListItemDto> cash = await service.GetPaymentsAsync(new PaymentFilter(null, null, null, PaymentMethod.Cash));
        Assert.Equal(2, cash.Count);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies transactional member workflows against SQL Server.</summary>
public sealed class MemberPersistenceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture fixture;

    /// <summary>Creates the member persistence test suite.</summary>
    public MemberPersistenceTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>Verifies rollback, unique DNI and preservation through lifecycle operations.</summary>
    [Fact]
    public async Task MemberLifecycle_IsTransactionalAndPreservesCurrentObligation()
    {
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        MemberService service = scope.ServiceProvider.GetRequiredService<MemberService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        CreateMemberModel model = CreateModel("12.345.678");

        BusinessRuleException missingRate = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(model));
        Assert.Equal("FEE_RATE_NOT_FOUND", missingRate.Code);
        Assert.Empty(await context.Members.ToListAsync());

        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 10, 1), 5000m, fixture.Clock.UtcNow));
        await context.SaveChangesAsync();
        await service.CreateAsync(model);
        Member member = await context.Members.Include(item => item.MembershipPeriods).SingleAsync();
        FeeObligation obligation = await context.FeeObligations.SingleAsync();
        long periodId = obligation.MembershipPeriodId;
        long rateId = obligation.FeeRateId;
        decimal amount = obligation.Amount;

        BusinessRuleException duplicate = await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(CreateModel("12345678")));
        Assert.Equal("DNI_DUPLICATE", duplicate.Code);

        await service.DeactivateAsync(member.Id);
        fixture.Clock.SetUtcNow(fixture.Clock.UtcNow.AddDays(1));
        ReactivateMemberResult result = await service.ReactivateAsync(member.Id);
        context.ChangeTracker.Clear();
        Member persisted = await context.Members.Include(item => item.MembershipPeriods).SingleAsync();
        FeeObligation persistedObligation = await context.FeeObligations.SingleAsync();

        Assert.False(result.ObligationCreated);
        Assert.Equal(MemberStatus.Active, persisted.Status);
        Assert.Equal(2, persisted.MembershipPeriods.Count);
        Assert.Equal(periodId, persistedObligation.MembershipPeriodId);
        Assert.Equal(rateId, persistedObligation.FeeRateId);
        Assert.Equal(amount, persistedObligation.Amount);
    }

    private static CreateMemberModel CreateModel(string dni) => new(dni,
        new MemberPersonalData("Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", "ana@example.com"));
}

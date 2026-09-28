using Microsoft.Extensions.DependencyInjection;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.Members.Dto;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Tests;

/// <summary>Verifies deterministic member pagination against SQL Server.</summary>
public sealed class MemberPaginationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture fixture;

    /// <summary>Creates the member pagination test suite.</summary>
    public MemberPaginationTests(DatabaseFixture fixture)
    {
        this.fixture = fixture;
    }

    /// <summary>Verifies that equal names are consistently ordered by identifier.</summary>
    [Fact]
    public async Task Search_UsesIdentifierAsFinalOrderingTieBreaker()
    {
        await using AsyncServiceScope scope = fixture.Services.CreateAsyncScope();
        MemberService service = scope.ServiceProvider.GetRequiredService<MemberService>();
        SociedadFomentoDbContext context = scope.ServiceProvider.GetRequiredService<SociedadFomentoDbContext>();
        context.FeeRates.Add(new FeeRate(new DateOnly(2026, 10, 1), 5000m, fixture.Clock.UtcNow));
        await context.SaveChangesAsync();
        await service.CreateAsync(CreateModel("30000101"));
        await service.CreateAsync(CreateModel("30000102"));
        await service.CreateAsync(CreateModel("30000103"));

        PagedResult<MemberSummaryDto> first = await service.SearchAsync(new MemberSearchCriteria(Page: 1, PageSize: 1));
        PagedResult<MemberSummaryDto> second = await service.SearchAsync(new MemberSearchCriteria(Page: 2, PageSize: 1));
        PagedResult<MemberSummaryDto> third = await service.SearchAsync(new MemberSearchCriteria(Page: 3, PageSize: 1));
        long[] ids = [first.Items.Single().Id, second.Items.Single().Id, third.Items.Single().Id];

        Assert.Equal(ids.OrderBy(id => id), ids);
        Assert.Equal(3, ids.Distinct().Count());
    }

    private static CreateMemberModel CreateModel(string dni) => new(dni,
        new MemberPersonalData("Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null));
}

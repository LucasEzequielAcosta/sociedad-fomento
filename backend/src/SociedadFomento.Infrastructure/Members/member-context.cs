using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.Members;
using SociedadFomento.Application.Members.Dto;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.Members;

internal sealed class MemberContext : IMemberContext
{
    private readonly SociedadFomentoDbContext dbContext;

    public MemberContext(SociedadFomentoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<bool> DniExistsAsync(string normalizedDni, CancellationToken cancellationToken) =>
        dbContext.Members.AnyAsync(member => member.Dni == normalizedDni, cancellationToken);

    public Task<Member?> FindMemberForLifecycleAsync(long memberId, CancellationToken cancellationToken) =>
        dbContext.Members.Include(member => member.MembershipPeriods)
            .SingleOrDefaultAsync(member => member.Id == memberId, cancellationToken);

    public Task<FeeRate?> FindApplicableFeeRateAsync(DateOnly period, CancellationToken cancellationToken) =>
        dbContext.FeeRates.Where(rate => rate.EffectiveMonth <= period)
            .OrderByDescending(rate => rate.EffectiveMonth).FirstOrDefaultAsync(cancellationToken);

    public Task<FeeObligation?> FindObligationAsync(long memberId, DateOnly period, CancellationToken cancellationToken) =>
        dbContext.FeeObligations.SingleOrDefaultAsync(
            obligation => obligation.MemberId == memberId && obligation.Period == period, cancellationToken);

    public Task<MemberDetailDto?> GetByIdAsync(long memberId, CancellationToken cancellationToken) =>
        ProjectDetails(dbContext.Members.AsNoTracking().Where(member => member.Id == memberId))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<MemberDetailDto?> GetByDniAsync(string normalizedDni, CancellationToken cancellationToken) =>
        ProjectDetails(dbContext.Members.AsNoTracking().Where(member => member.Dni == normalizedDni))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<MemberSummaryDto>> SearchAsync(MemberSearchCriteria criteria, CancellationToken cancellationToken)
    {
        IQueryable<Member> query = ApplyFilters(dbContext.Members.AsNoTracking(), criteria);
        int totalCount = await query.CountAsync(cancellationToken);
        List<MemberSummaryDto> items = await query.OrderBy(member => member.LastName).ThenBy(member => member.FirstName)
            .ThenBy(member => member.Id).Skip((criteria.Page - 1) * criteria.PageSize).Take(criteria.PageSize)
            .Select(member => new MemberSummaryDto(member.Id, member.Dni, member.FirstName, member.LastName, member.Status))
            .ToListAsync(cancellationToken);
        return new PagedResult<MemberSummaryDto>(items, criteria.Page, criteria.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<MembershipPeriodDto>?> GetHistoryAsync(long memberId, CancellationToken cancellationToken)
    {
        bool exists = await dbContext.Members.AsNoTracking().AnyAsync(member => member.Id == memberId, cancellationToken);
        if (!exists)
        {
            return null;
        }

        return await dbContext.MembershipPeriods.AsNoTracking().Where(period => period.MemberId == memberId)
            .OrderBy(period => period.StartDate)
            .Select(period => new MembershipPeriodDto(period.Id, period.StartDate, period.EndDate))
            .ToListAsync(cancellationToken);
    }

    public void AddMember(Member member) => dbContext.Members.Add(member);
    public void AddObligation(FeeObligation obligation) => dbContext.FeeObligations.Add(obligation);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw MemberStateConflict();
        }
        catch (DbUpdateException exception) when (IsDniConflict(exception))
        {
            throw new BusinessRuleException("DNI_DUPLICATE", "A member with the supplied DNI already exists.");
        }
        catch (DbUpdateException exception) when (IsMemberStateConflict(exception))
        {
            throw MemberStateConflict();
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            T result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static IQueryable<MemberDetailDto> ProjectDetails(IQueryable<Member> query) => query
        .Select(member => new MemberDetailDto(
            member.Id, member.Dni, member.FirstName, member.LastName, member.Address, member.BirthDate,
            member.Phone, member.Email, member.RegistrationDate, member.Status));

    private static IQueryable<Member> ApplyFilters(IQueryable<Member> query, MemberSearchCriteria criteria)
    {
        if (criteria.Dni is not null)
        {
            query = query.Where(member => member.Dni.Contains(criteria.Dni));
        }
        if (criteria.Name is not null)
        {
            query = query.Where(member => member.FirstName.Contains(criteria.Name));
        }
        if (criteria.LastName is not null)
        {
            query = query.Where(member => member.LastName.Contains(criteria.LastName));
        }
        return query;
    }

    private static bool IsDniConflict(DbUpdateException exception) =>
        IsUniqueConstraint(exception, "IX_Members_Dni");

    private static bool IsMemberStateConflict(DbUpdateException exception) =>
        IsUniqueConstraint(exception, "IX_MembershipPeriods_MemberId") ||
        IsUniqueConstraint(exception, "IX_FeeObligations_MemberId_Period");

    private static bool IsUniqueConstraint(DbUpdateException exception, string constraintName) =>
        exception.InnerException is SqlException sqlException &&
        sqlException.Number is 2601 or 2627 &&
        sqlException.Message.Contains(constraintName, StringComparison.OrdinalIgnoreCase);

    private static BusinessRuleException MemberStateConflict() => new(
        "MEMBER_STATE_CONFLICT", "The member state changed concurrently. Reload the member and retry.");
}

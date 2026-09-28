using SociedadFomento.Application.Members;
using SociedadFomento.Application.Members.Dto;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

internal sealed class FakeMemberContext : IMemberContext
{
    internal List<Member> Members { get; } = [];
    internal List<FeeRate> FeeRates { get; } = [];
    internal List<FeeObligation> Obligations { get; } = [];

    public Task<bool> DniExistsAsync(string normalizedDni, CancellationToken cancellationToken) =>
        Task.FromResult(Members.Any(member => member.Dni == normalizedDni));

    public Task<Member?> FindMemberForLifecycleAsync(long memberId, CancellationToken cancellationToken) =>
        Task.FromResult(Members.SingleOrDefault());

    public Task<FeeRate?> FindApplicableFeeRateAsync(DateOnly period, CancellationToken cancellationToken) =>
        Task.FromResult(FeeRates.Where(rate => rate.EffectiveMonth <= period).OrderByDescending(rate => rate.EffectiveMonth).FirstOrDefault());

    public Task<FeeObligation?> FindObligationAsync(long memberId, DateOnly period, CancellationToken cancellationToken) =>
        Task.FromResult(Obligations.SingleOrDefault(obligation => obligation.Period == period));

    public Task<MemberDetailDto?> GetByIdAsync(long memberId, CancellationToken cancellationToken) =>
        Task.FromResult(Members.Select(Map).SingleOrDefault());

    public Task<MemberDetailDto?> GetByDniAsync(string normalizedDni, CancellationToken cancellationToken) =>
        Task.FromResult(Members.Where(member => member.Dni == normalizedDni).Select(Map).SingleOrDefault());

    public Task<PagedResult<MemberSummaryDto>> SearchAsync(MemberSearchCriteria criteria, CancellationToken cancellationToken)
    {
        List<MemberSummaryDto> items = Members.Select(member => new MemberSummaryDto(
            member.Id, member.Dni, member.FirstName, member.LastName, member.Status)).ToList();
        return Task.FromResult(new PagedResult<MemberSummaryDto>(items, criteria.Page, criteria.PageSize, items.Count));
    }

    public Task<IReadOnlyList<MembershipPeriodDto>?> GetHistoryAsync(long memberId, CancellationToken cancellationToken)
    {
        Member? member = Members.SingleOrDefault();
        IReadOnlyList<MembershipPeriodDto>? history = member?.MembershipPeriods
            .Select(period => new MembershipPeriodDto(period.Id, period.StartDate, period.EndDate)).ToList();
        return Task.FromResult(history);
    }

    public void AddMember(Member member) => Members.Add(member);
    public void AddObligation(FeeObligation obligation) => Obligations.Add(obligation);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken) =>
        operation(cancellationToken);

    private static MemberDetailDto Map(Member member) => new(
        member.Id, member.Dni, member.FirstName, member.LastName, member.Address, member.BirthDate,
        member.Phone, member.Email, member.RegistrationDate, member.Status);
}

using SociedadFomento.Application.Members.Dto;
using SociedadFomento.Application.Members.Models;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Members;

/// <summary>Defines member persistence operations required by the application service.</summary>
public interface IMemberContext
{
    Task<bool> DniExistsAsync(string normalizedDni, CancellationToken cancellationToken);
    Task<Member?> FindMemberForLifecycleAsync(long memberId, CancellationToken cancellationToken);
    Task<FeeRate?> FindApplicableFeeRateAsync(DateOnly period, CancellationToken cancellationToken);
    Task<FeeObligation?> FindObligationAsync(long memberId, DateOnly period, CancellationToken cancellationToken);
    Task<MemberDetailDto?> GetByIdAsync(long memberId, CancellationToken cancellationToken);
    Task<MemberDetailDto?> GetByDniAsync(string normalizedDni, CancellationToken cancellationToken);
    Task<PagedResult<MemberSummaryDto>> SearchAsync(MemberSearchCriteria criteria, CancellationToken cancellationToken);
    Task<IReadOnlyList<MembershipPeriodDto>?> GetHistoryAsync(long memberId, CancellationToken cancellationToken);
    void AddMember(Member member);
    void AddObligation(FeeObligation obligation);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}

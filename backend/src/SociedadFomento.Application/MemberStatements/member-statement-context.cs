using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.MemberStatements;

/// <summary>Defines persistence operations required to build a member account statement.</summary>
public interface IMemberStatementContext
{
    Task<Member?> GetMemberAsync(long memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<FeeObligation>> GetObligationsAsync(long memberId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Payment>> GetPaymentsAsync(long memberId, CancellationToken cancellationToken);
}

using SociedadFomento.Application.MemberStatements;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

internal sealed class FakeMemberStatementContext : IMemberStatementContext
{
    internal List<Member> Members { get; } = [];
    internal List<FeeObligation> Obligations { get; } = [];
    internal List<Payment> Payments { get; } = [];

    public Task<Member?> GetMemberAsync(long memberId, CancellationToken cancellationToken) =>
        Task.FromResult(Members.SingleOrDefault(member => member.Id == memberId));

    public Task<IReadOnlyList<FeeObligation>> GetObligationsAsync(long memberId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<FeeObligation>>(Obligations.Where(obligation => obligation.MemberId == memberId).ToList());

    public Task<IReadOnlyList<Payment>> GetPaymentsAsync(long memberId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Payment>>(Payments.Where(payment => payment.MemberId == memberId).ToList());
}

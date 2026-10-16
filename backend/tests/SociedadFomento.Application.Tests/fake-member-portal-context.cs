using SociedadFomento.Application.MemberPortal;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.Tests;

internal sealed class FakeMemberPortalContext : IMemberPortalContext
{
    internal List<Member> Members { get; } = [];

    public Task<Member?> FindByDniAsync(string normalizedDni, CancellationToken cancellationToken) =>
        Task.FromResult(Members.SingleOrDefault(member => member.Dni == normalizedDni));
}

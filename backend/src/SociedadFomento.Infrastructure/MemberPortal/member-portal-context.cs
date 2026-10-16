using Microsoft.EntityFrameworkCore;
using SociedadFomento.Application.MemberPortal;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Infrastructure.Persistence;

namespace SociedadFomento.Infrastructure.MemberPortal;

internal sealed class MemberPortalContext : IMemberPortalContext
{
    private readonly SociedadFomentoDbContext dbContext;

    public MemberPortalContext(SociedadFomentoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<Member?> FindByDniAsync(string normalizedDni, CancellationToken cancellationToken) =>
        dbContext.Members.SingleOrDefaultAsync(member => member.Dni == normalizedDni, cancellationToken);
}

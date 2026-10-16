using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.MemberPortal;

/// <summary>Defines member persistence operations required by the member portal.</summary>
public interface IMemberPortalContext
{
    /// <summary>Finds a member by its normalized DNI.</summary>
    Task<Member?> FindByDniAsync(string normalizedDni, CancellationToken cancellationToken);
}

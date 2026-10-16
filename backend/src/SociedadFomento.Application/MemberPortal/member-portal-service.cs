using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.MemberPortal.Dto;
using SociedadFomento.Domain.Entities;

namespace SociedadFomento.Application.MemberPortal;

/// <summary>Coordinates member portal authentication using only the member DNI.</summary>
public sealed class MemberPortalService
{
    private readonly IMemberPortalContext context;

    /// <summary>Creates the member portal service.</summary>
    public MemberPortalService(IMemberPortalContext context)
    {
        this.context = context;
    }

    /// <summary>Resolves the member identity for the supplied DNI, or null when it does not exist.</summary>
    public async Task<MemberPortalIdentityDto?> LoginAsync(string dni, CancellationToken cancellationToken = default)
    {
        string normalizedDni;
        try
        {
            normalizedDni = DniNormalizer.Normalize(dni);
        }
        catch (ArgumentException exception)
        {
            throw new RequestValidationException("INVALID_MEMBER_DNI", exception.Message);
        }

        Member? member = await context.FindByDniAsync(normalizedDni, cancellationToken);
        return member is null
            ? null
            : new MemberPortalIdentityDto(member.Id, member.Dni, member.FirstName, member.LastName, member.Status);
    }
}

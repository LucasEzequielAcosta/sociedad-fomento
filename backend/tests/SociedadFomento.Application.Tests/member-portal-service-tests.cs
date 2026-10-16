using SociedadFomento.Application.Common.Exceptions;
using SociedadFomento.Application.MemberPortal;
using SociedadFomento.Application.MemberPortal.Dto;
using SociedadFomento.Domain.Entities;
using SociedadFomento.Domain.Enums;

namespace SociedadFomento.Application.Tests;

/// <summary>Tests member portal login by DNI.</summary>
public sealed class MemberPortalServiceTests
{
    /// <summary>Verifies that a formatted DNI is normalized and resolves the member identity.</summary>
    [Fact]
    public async Task Login_NormalizesDniAndReturnsIdentity()
    {
        FakeMemberPortalContext context = new();
        Member member = new("12345678", "Ana", "Pérez", "Calle 1", new DateOnly(1990, 1, 1), "123", null, new DateOnly(2026, 9, 1));
        context.Members.Add(member);
        MemberPortalService service = new(context);

        MemberPortalIdentityDto? identity = await service.LoginAsync("12.345.678");

        Assert.NotNull(identity);
        Assert.Equal("12345678", identity.Dni);
        Assert.Equal("Ana", identity.FirstName);
        Assert.Equal(MemberStatus.Active, identity.Status);
    }

    /// <summary>Verifies that an unknown DNI returns null instead of throwing.</summary>
    [Fact]
    public async Task Login_UnknownDniReturnsNull()
    {
        MemberPortalService service = new(new FakeMemberPortalContext());

        MemberPortalIdentityDto? identity = await service.LoginAsync("99999999");

        Assert.Null(identity);
    }

    /// <summary>Verifies that an empty DNI is rejected as invalid input.</summary>
    [Fact]
    public async Task Login_EmptyDniThrowsValidation()
    {
        MemberPortalService service = new(new FakeMemberPortalContext());

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.LoginAsync("   "));

        Assert.Equal("INVALID_MEMBER_DNI", exception.Code);
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SociedadFomento.Api.Authentication;
using SociedadFomento.Api.Contracts.MemberPortal;
using SociedadFomento.Api.Contracts.MemberStatements;
using SociedadFomento.Application.Abstractions;
using SociedadFomento.Application.MemberPortal;
using SociedadFomento.Application.MemberPortal.Dto;
using SociedadFomento.Application.MemberStatements;
using SociedadFomento.Application.MemberStatements.Dto;
using SociedadFomento.Application.MemberStatements.Models;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes the member self-service portal identified by DNI.</summary>
[ApiController]
[Route("api/portal")]
public sealed class MemberPortalController : ControllerBase
{
    private readonly MemberPortalService memberPortalService;
    private readonly MemberStatementService memberStatementService;
    private readonly IClock clock;
    private readonly IAntiforgery antiforgery;

    /// <summary>Creates the member portal controller.</summary>
    public MemberPortalController(
        MemberPortalService memberPortalService,
        MemberStatementService memberStatementService,
        IClock clock,
        IAntiforgery antiforgery)
    {
        this.memberPortalService = memberPortalService;
        this.memberStatementService = memberStatementService;
        this.clock = clock;
        this.antiforgery = antiforgery;
    }

    /// <summary>Creates a member session when the supplied DNI belongs to a member.</summary>
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AuthenticationConstants.MemberLoginRateLimitPolicy)]
    [HttpPost("login")]
    public async Task<IActionResult> Login(MemberLoginRequest request, CancellationToken cancellationToken)
    {
        MemberPortalIdentityDto? identity = await memberPortalService.LoginAsync(request.Dni, cancellationToken);
        if (identity is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Credenciales inválidas",
                Extensions = { ["code"] = "INVALID_CREDENTIALS" }
            });
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, identity.MemberId.ToString()),
            new(ClaimTypes.Role, "Member")
        ];
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, AuthenticationConstants.MemberScheme));
        await HttpContext.SignInAsync(AuthenticationConstants.MemberScheme, principal, new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = false
        });
        HttpContext.User = principal;
        RegenerateXsrfCookie();
        return NoContent();
    }

    /// <summary>Deletes the current member session.</summary>
    [Authorize(AuthenticationSchemes = AuthenticationConstants.MemberScheme, Roles = "Member")]
    [ValidateAntiForgeryToken]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthenticationConstants.MemberScheme);
        return NoContent();
    }

    /// <summary>Returns the minimum identity of the authenticated member.</summary>
    [Authorize(AuthenticationSchemes = AuthenticationConstants.MemberScheme, Roles = "Member")]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentMemberResponse>> Me(CancellationToken cancellationToken)
    {
        MemberStatementDto statement = await GetOwnStatementAsync(cancellationToken);
        return Ok(new CurrentMemberResponse(
            statement.MemberId, statement.Dni, statement.FirstName, statement.LastName, statement.Status.ToString()));
    }

    /// <summary>Returns the account statement of the authenticated member as of today.</summary>
    [Authorize(AuthenticationSchemes = AuthenticationConstants.MemberScheme, Roles = "Member")]
    [HttpGet("statement")]
    public async Task<ActionResult<MemberStatementResponse>> Statement(CancellationToken cancellationToken)
    {
        MemberStatementDto statement = await GetOwnStatementAsync(cancellationToken);
        return Ok(new MemberStatementResponse(
            statement.MemberId,
            statement.Dni,
            statement.FirstName,
            statement.LastName,
            statement.Status.ToString(),
            statement.Obligations,
            statement.Payments,
            statement.TotalPaid,
            statement.TotalDebt));
    }

    private void RegenerateXsrfCookie()
    {
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append(AuthenticationConstants.XsrfCookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true
        });
    }

    private async Task<MemberStatementDto> GetOwnStatementAsync(CancellationToken cancellationToken)
    {
        long memberId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await memberStatementService.GetAsync(new GetMemberStatementModel(memberId, clock.Today), cancellationToken);
    }
}

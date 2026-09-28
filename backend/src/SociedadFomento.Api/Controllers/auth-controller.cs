using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SociedadFomento.Api.Authentication;
using SociedadFomento.Api.Contracts.Authentication;
using SociedadFomento.Application.Authentication;
using SociedadFomento.Application.Authentication.Dto;
using SociedadFomento.Application.Authentication.Models;

namespace SociedadFomento.Api.Controllers;

/// <summary>Exposes administrator session operations.</summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AdminAuthenticationService authenticationService;
    private readonly IAntiforgery antiforgery;

    /// <summary>Creates the authentication controller.</summary>
    public AuthController(AdminAuthenticationService authenticationService, IAntiforgery antiforgery)
    {
        this.authenticationService = authenticationService;
        this.antiforgery = antiforgery;
    }

    /// <summary>Issues the antiforgery cookies used by Angular.</summary>
    [AllowAnonymous]
    [HttpGet("antiforgery")]
    public IActionResult Antiforgery()
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
        return NoContent();
    }

    /// <summary>Creates an administrator session when credentials are valid.</summary>
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(AuthenticationConstants.LoginRateLimitPolicy)]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        AdminLoginResult result = await authenticationService.LoginAsync(request.Email, request.Password, cancellationToken);
        if (!result.Succeeded)
        {
            return Unauthorized(CreateProblem("Invalid credentials", "INVALID_CREDENTIALS"));
        }

        ClaimsPrincipal principal = CreatePrincipal(result.Identity!);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = false
        });
        return NoContent();
    }

    /// <summary>Deletes the current administrator session.</summary>
    [Authorize]
    [ValidateAntiForgeryToken]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>Returns the minimum current administrator identity.</summary>
    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentAdminResponse> Me()
    {
        long id = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        string email = User.FindFirstValue(ClaimTypes.Email)!;
        string role = User.FindFirstValue(ClaimTypes.Role)!;
        return Ok(new CurrentAdminResponse(id, email, role));
    }

    private static ClaimsPrincipal CreatePrincipal(AdminIdentityDto identity)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, identity.Id.ToString()),
            new(ClaimTypes.Email, identity.Email),
            new(ClaimTypes.Role, identity.Role.ToString())
        ];
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private static ProblemDetails CreateProblem(string title, string code) => new()
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = title,
        Extensions = { ["code"] = code }
    };
}

namespace SociedadFomento.Api.Authentication;

/// <summary>Defines stable authentication scheme and cookie names.</summary>
public static class AuthenticationConstants
{
    public const string CookieName = "SociedadFomento.Admin";
    public const string LoginRateLimitPolicy = "admin-login";
    public const string XsrfCookieName = "XSRF-TOKEN";
    public const string XsrfHeaderName = "X-XSRF-TOKEN";
}

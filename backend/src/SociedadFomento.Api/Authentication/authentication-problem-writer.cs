using Microsoft.AspNetCore.Mvc;

namespace SociedadFomento.Api.Authentication;

/// <summary>Writes safe authentication and authorization problems.</summary>
public static class AuthenticationProblemWriter
{
    /// <summary>Writes a stable problem response.</summary>
    public static Task WriteAsync(HttpContext context, int status, string title, string code)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Extensions = { ["code"] = code }
        });
    }
}

using Microsoft.AspNetCore.Mvc;

namespace SociedadFomento.Api.Errors;

/// <summary>Creates the uniform validation response used by automatic API model validation.</summary>
public static class InvalidModelStateResponseFactory
{
    /// <summary>Creates a 400 validation problem while preserving errors by field.</summary>
    public static IActionResult Create(ActionContext context)
    {
        ValidationProblemDetails problem = new(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Invalid request"
        };
        problem.Extensions["code"] = "INVALID_REQUEST";
        return new BadRequestObjectResult(problem);
    }
}

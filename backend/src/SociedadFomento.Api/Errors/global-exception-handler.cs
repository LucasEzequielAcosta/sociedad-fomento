using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SociedadFomento.Application.Common.Exceptions;

namespace SociedadFomento.Api.Errors;

/// <summary>Converts application failures into stable HTTP problem responses.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> logger;

    /// <summary>Creates the global exception handler.</summary>
    public GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    {
        this.problemDetailsService = problemDetailsService;
        this.logger = logger;
    }

    /// <summary>Writes a safe problem response for the supplied exception.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int status, string title, string code) = Map(exception);
        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Ocurrió un error inesperado en la aplicación.");
        }

        ProblemDetails problem = new() { Status = status, Title = title, Detail = SafeDetail(exception, status) };
        problem.Extensions["code"] = code;
        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static (int Status, string Title, string Code) Map(Exception exception) => exception switch
    {
        RequestValidationException validation => (StatusCodes.Status400BadRequest, "Solicitud inválida", validation.Code),
        ResourceNotFoundException notFound => (StatusCodes.Status404NotFound, "Recurso no encontrado", notFound.Code),
        BusinessRuleException conflict => (StatusCodes.Status409Conflict, "Conflicto de regla de negocio", conflict.Code),
        AntiforgeryValidationException => (StatusCodes.Status400BadRequest, "Token de seguridad inválido", "INVALID_ANTIFORGERY_TOKEN"),
        _ => (StatusCodes.Status500InternalServerError, "Error inesperado", "UNEXPECTED_ERROR")
    };

    private static string SafeDetail(Exception exception, int status) => status == StatusCodes.Status500InternalServerError
        ? "Ocurrió un error inesperado."
        : exception.Message;
}

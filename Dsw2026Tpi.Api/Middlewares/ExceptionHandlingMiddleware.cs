using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Models;
using Dsw2026Tpi.CrossCutting.Resources;
using System.Net;
using System.Text.Json;

namespace Dsw2026Tpi.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (exception is AppException)
            {
                _logger.LogWarning(
                    "Solicitud rechazada: {ExceptionType} - {Message}",
                    exception.GetType().Name,
                    exception.Message);
            }
            else
            {
                _logger.LogError(
                    exception,
                    "Se produjo un error no controlado durante la solicitud");
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if(context.Response.HasStarted)
        {
            throw exception;
        }

        ErrorResponse error = exception is AppException appException ? appException.Error : new ErrorResponse(
            nameof(ErrorCodes.UNHANDLED_ERROR),
            ErrorCodes.UNHANDLED_ERROR
            
            );

        HttpStatusCode status = exception switch
        {
            ValidationException or BusinessRuleException => HttpStatusCode.BadRequest,
            EntityNotFoundException => HttpStatusCode.NotFound,
            ConflictException => HttpStatusCode.Conflict,
            AuthenticationException => HttpStatusCode.Unauthorized,
            AuthorizationException => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.InternalServerError
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int) status;

        await context.Response.WriteAsync(

            JsonSerializer.Serialize(error, JsonOptions)

         );
    }
}

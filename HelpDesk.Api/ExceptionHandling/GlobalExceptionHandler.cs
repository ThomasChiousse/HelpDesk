using HelpDesk.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HelpDesk.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            KeyNotFoundException =>
                StatusCodes.Status404NotFound,

            ArgumentException =>
                StatusCodes.Status400BadRequest,

            AssigneeMismatchException =>
                StatusCodes.Status409Conflict,

            TicketHasNoAssigneeException =>
                StatusCodes.Status409Conflict,

            TicketClosedException =>
                StatusCodes.Status409Conflict,

            TicketAlreadyClosedException =>
                StatusCodes.Status409Conflict,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = statusCode switch
                    {
                        StatusCodes.Status404NotFound =>
                            "Resource not found",

                        StatusCodes.Status400BadRequest =>
                            "Invalid request",

                        StatusCodes.Status409Conflict =>
                            "Conflict",

                        _ =>
                            "An unexpected error occurred"
                    },

                    Detail = statusCode == 500
                        ? null
                        : exception.Message
                }
            });
    }
}
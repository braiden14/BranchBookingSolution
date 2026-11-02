using BranchBooking.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BranchBooking.Api.Middleware
{
    public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var traceId = httpContext.TraceIdentifier;

            var (statusCode, title, detail, type) = exception switch
            {
                AuthenticationException => (StatusCodes.Status401Unauthorized, "Authentication failed.",
                    exception.Message, "https://httpstatuses.com/401"),
                DomainValidationException => (StatusCodes.Status400BadRequest, "Validation failed.",
                    exception.Message, "https://httpstatuses.com/400"),
                DomainException => (StatusCodes.Status422UnprocessableEntity, "The request could not be processed.",
                    exception.Message, "https://httpstatuses.com/422"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.",
                    "The request could not be completed.", "https://httpstatuses.com/500")
            };

            if (exception is DomainException)
            {
                logger.LogWarning(
                    "Domain exception while processing {HttpMethod} {RequestPath}. TraceId: {TraceId}. Message: {Message}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    traceId,
                    exception.Message);
            }
            else
            {
                logger.LogError(
                    exception,
                    "Unhandled exception while processing {HttpMethod} {RequestPath}. TraceId: {TraceId}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    traceId);
            }

            httpContext.Response.StatusCode = statusCode;

            await problemDetailsService.WriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    Exception = exception,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = statusCode,
                        Title = title,
                        Detail = detail,
                        Instance = httpContext.Request.Path,
                        Type = type
                    }
                });

            return true;
        }
    }
}

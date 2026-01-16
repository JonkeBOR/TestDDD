using System.Net;
using System.Text.Json;
using TestDDD.Models;

namespace TestDDD.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var correlationId = context.TraceIdentifier;
        var response = new ErrorResponse 
        { 
            Error = "An unexpected error occurred while processing the request.",
            CorrelationId = correlationId
        };

        switch (exception)
        {
            case ArgumentException argEx:
                _logger.LogWarning(
                    argEx,
                    "Validation error: {Message}. CorrelationId: {CorrelationId}",
                    argEx.Message,
                    correlationId);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Error = "Invalid request parameters.";
                break;

            case InvalidOperationException invalidOpEx:
                _logger.LogWarning(
                    invalidOpEx,
                    "Business logic error - Customer data not found. CorrelationId: {CorrelationId}",
                    correlationId);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Error = "Customer data not found for the provided SSN.";
                break;

            case HttpRequestException httpReqEx when httpReqEx.StatusCode == HttpStatusCode.NotFound:
                _logger.LogWarning(
                    httpReqEx,
                    "External API returned 404 - Resource not found. CorrelationId: {CorrelationId}",
                    correlationId);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Error = "Customer data not found for the provided SSN.";
                break;

            case HttpRequestException httpReqEx:
                _logger.LogError(
                    httpReqEx,
                    "External API request failed with status code: {StatusCode}. CorrelationId: {CorrelationId}",
                    httpReqEx.StatusCode,
                    correlationId);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                response.Error = "External service is temporarily unavailable. Please try again later.";
                break;

            case TimeoutException timeoutEx:
                _logger.LogError(
                    timeoutEx,
                    "Request timeout occurred. CorrelationId: {CorrelationId}",
                    correlationId);
                context.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                response.Error = "Request timeout. Please try again later.";
                break;

            default:
                _logger.LogError(
                    exception,
                    "Unexpected system error occurred. Exception Type: {ExceptionType}. CorrelationId: {CorrelationId}. StackTrace: {StackTrace}",
                    exception.GetType().Name,
                    correlationId,
                    exception.StackTrace);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Error = "An unexpected error occurred while processing the request.";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

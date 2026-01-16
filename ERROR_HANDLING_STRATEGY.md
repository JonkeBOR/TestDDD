# Error Handling & Logging Strategy

## Overview

This application implements a comprehensive error handling and logging strategy that ensures:
- **User-facing errors** are communicated clearly without exposing internal details
- **System-level errors** are logged with full context for troubleshooting
- **Request traceability** through correlation IDs

## Architecture

### 1. Exception Handling Middleware

The `ExceptionHandlingMiddleware` is the central point for catching and handling all unhandled exceptions.

**Location:** `TestDDD/Middleware/ExceptionHandlingMiddleware.cs`

**Features:**
- Catches all unhandled exceptions at the application level
- Logs errors with appropriate severity levels
- Returns user-friendly error messages without exposing internal details
- Includes correlation IDs for request tracing

### 2. Error Classification

Exceptions are categorized and handled as follows:

#### User Input Errors (BadRequest - 400)
- **Exception Type:** `ArgumentException`
- **User Message:** "Invalid request parameters."
- **Log Level:** Warning
- **Visibility:** Safe to expose to users

#### Business Logic Errors (NotFound - 404)
- **Exception Type:** `InvalidOperationException` (e.g., customer not found)
- **User Message:** "Customer data not found for the provided SSN."
- **Log Level:** Warning
- **Visibility:** Safe to expose to users

#### External API Errors (503/504)
- **Exception Type:** `HttpRequestException`, `TimeoutException`
- **User Messages:**
  - 404 from API: "Customer data not found for the provided SSN." (404)
  - Other HTTP errors: "External service is temporarily unavailable." (503)
  - Timeout: "Request timeout. Please try again later." (504)
- **Log Level:** Error
- **Visibility:** Generic messages to avoid exposing API details

#### System Errors (InternalServerError - 500)
- **Exception Type:** All other exceptions
- **User Message:** "An unexpected error occurred while processing the request."
- **Log Level:** Error (with full stack trace)
- **Visibility:** No internal details exposed

### 3. Logging Configuration

#### Development Environment
- **Log Level:** Information (verbose)
- **Output:** Console + Rolling File
- **File Location:** `logs/kyc-service-*.txt` (daily rotation)
- **Retention:** No limit (useful for debugging)

#### Production Environment
- **Log Level:** Warning (less verbose)
- **Output:** Console + Rolling File
- **File Location:** `logs/kyc-service-*.txt` (daily rotation)
- **Retention:** Last 30 days

#### Log Output Template
```
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}
{Exception}
```

### 4. Correlation ID Tracking

Each error response includes a `CorrelationId` that matches the `HttpContext.TraceIdentifier`:

```json
{
  "error": "Customer data not found for the provided SSN.",
  "correlationId": "0HN1GIMPE4NTP:00000001"
}
```

This allows users and support staff to search logs for the specific request:
```bash
grep "0HN1GIMPE4NTP:00000001" logs/kyc-service-*.txt
```

### 5. Error Response Model

**Location:** `TestDDD/Models/ErrorResponse.cs`

```csharp
public class ErrorResponse
{
    public string Error { get; set; }           // User-friendly error message
    public string? CorrelationId { get; set; }  // Request correlation ID for tracing
}
```

## Best Practices Applied

### User-Facing Error Messages
✅ Generic and non-technical language
✅ No exposure of internal implementation details
✅ No database or API endpoint information
✅ Actionable guidance (e.g., "Please try again later")

### System-Level Logging
✅ Full exception details with stack traces
✅ Correlation IDs for request tracking
✅ Contextual information (exception type, method)
✅ Environment indicators (Development/Production)
✅ Timestamp with timezone information

### Sensitive Information Protection
✅ No credentials logged
✅ No personal data in error messages
✅ No SQL queries or internal API URLs exposed
✅ Internal details only logged at Error level in development

## Integration Points

### Middleware Pipeline (Program.cs)
```csharp
app.UseExceptionHandling();  // Must be first in middleware pipeline
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
```

### Controller Error Handling
Controllers validate user input and return appropriate status codes:

```csharp
if (string.IsNullOrWhiteSpace(ssn))
{
    _logger.LogWarning("Invalid SSN provided: empty or whitespace");
    return BadRequest(new ErrorResponse { Error = "SSN cannot be empty." });
}
```

**Exception handling for business logic is delegated to the middleware.**

## Monitoring and Debugging

### View Logs in Real-Time
```bash
tail -f logs/kyc-service-2024-01-15.txt
```

### Search for Specific Errors
```bash
grep -i "correlation" logs/kyc-service-*.txt
grep "ERROR" logs/kyc-service-*.txt
```

### Filter by Severity
- **Warning:** Business logic issues, external service problems
- **Error:** System failures, unexpected exceptions

## Future Enhancements

1. **Structured Logging:** Implement JSON-based logging for better parsing
2. **Log Aggregation:** Integrate with ELK Stack or Application Insights
3. **Alerting:** Set up alerts for error thresholds
4. **Metrics:** Add distributed tracing with W3C Trace Context
5. **Sanitization:** Implement automatic PII redaction in logs

## References

- Serilog Documentation: https://serilog.net/
- ASP.NET Core Error Handling: https://docs.microsoft.com/aspnet/core/fundamentals/error-handling
- Structured Logging Best Practices: https://github.com/serilog/serilog/wiki

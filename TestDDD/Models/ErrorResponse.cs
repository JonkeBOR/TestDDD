namespace TestDDD.Models;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracing this error in logs
    /// </summary>
    public string? CorrelationId { get; set; }
}

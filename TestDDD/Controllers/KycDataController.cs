using Microsoft.AspNetCore.Mvc;
using TestDDD.Models;
using TestDDD.Services;

namespace TestDDD.Controllers;

[ApiController]
[Route("")]
public class KycDataController : ControllerBase
{
    private readonly IKycAggregationService _kycAggregationService;
    private readonly ILogger<KycDataController> _logger;

    public KycDataController(IKycAggregationService kycAggregationService, ILogger<KycDataController> logger)
    {
        _kycAggregationService = kycAggregationService;
        _logger = logger;
    }

    /// <summary>
    /// Get aggregated KYC data for a customer by SSN
    /// </summary>
    /// <param name="ssn">Social Security Number of the customer</param>
    /// <returns>Aggregated KYC data</returns>
    [HttpGet("kyc-data/{ssn}")]
    public async Task<IActionResult> GetAggregatedKycData(string ssn)
    {
        if (string.IsNullOrWhiteSpace(ssn))
        {
            _logger.LogWarning("Invalid SSN provided: empty or whitespace");
            return BadRequest(new ErrorResponse { Error = "SSN cannot be empty." });
        }

        try
        {
            _logger.LogInformation("Processing request for KYC data with SSN: {Ssn}", ssn);
            var kycData = await _kycAggregationService.GetAggregatedKycDataAsync(ssn);
            return Ok(kycData);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Customer data not found for SSN: {Ssn}", ssn);
            return NotFound(new ErrorResponse { Error = "Customer data not found for the provided SSN." });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "External API returned 404 for SSN: {Ssn}", ssn);
            return NotFound(new ErrorResponse { Error = "Customer data not found for the provided SSN." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while processing KYC data request for SSN: {Ssn}", ssn);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse { Error = "An unexpected error occurred while processing the request." });
        }
    }
}

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}

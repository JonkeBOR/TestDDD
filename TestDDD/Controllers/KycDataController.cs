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

            _logger.LogInformation("Processing request for KYC data with SSN: {Ssn}", ssn);
            var kycData = await _kycAggregationService.GetAggregatedKycDataAsync(ssn);
            return Ok(kycData);
        }
    }

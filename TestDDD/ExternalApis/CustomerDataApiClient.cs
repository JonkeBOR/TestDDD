using System.Net.Http.Json;
using TestDDD.ExternalApis.Dtos;

namespace TestDDD.ExternalApis;

/// <summary>
/// Client for interacting with the Customer Data API
/// </summary>
public interface ICustomerDataApiClient
{
    Task<PersonalDetailsDto?> GetPersonalDetailsAsync(string ssn);
    Task<ContactDetailsDto?> GetContactDetailsAsync(string ssn);
    Task<KycFormDto?> GetKycFormAsync(string ssn, DateTime asOfDate);
}

public class CustomerDataApiClient : ICustomerDataApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerDataApiClient> _logger;
    private const string BaseUrl = "https://customerdataapi.azurewebsites.net/api";

    public CustomerDataApiClient(HttpClient httpClient, ILogger<CustomerDataApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PersonalDetailsDto?> GetPersonalDetailsAsync(string ssn)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/personal-details/{ssn}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Personal details not found for SSN: {Ssn}", ssn);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PersonalDetailsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching personal details for SSN: {Ssn}", ssn);
            throw;
        }
    }

    public async Task<ContactDetailsDto?> GetContactDetailsAsync(string ssn)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/contact-details/{ssn}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Contact details not found for SSN: {Ssn}", ssn);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ContactDetailsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching contact details for SSN: {Ssn}", ssn);
            throw;
        }
    }

    public async Task<KycFormDto?> GetKycFormAsync(string ssn, DateTime asOfDate)
    {
        try
        {
            var dateString = asOfDate.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync($"{BaseUrl}/kyc-form/{ssn}/{dateString}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("KYC form not found for SSN: {Ssn}, Date: {Date}", ssn, dateString);
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<KycFormDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching KYC form for SSN: {Ssn}", ssn);
            throw;
        }
    }
}

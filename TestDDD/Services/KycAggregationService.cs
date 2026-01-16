using Microsoft.Extensions.Caching.Memory;
using TestDDD.Data;
using TestDDD.ExternalApis;
using TestDDD.ExternalApis.Dtos;
using TestDDD.Models;

namespace TestDDD.Services;

/// <summary>
/// Service for aggregating KYC data with persistent caching
/// </summary>
public interface IKycAggregationService
{
    Task<AggregatedKycData> GetAggregatedKycDataAsync(string ssn);
}

public class KycAggregationService : IKycAggregationService
{
    private readonly ICustomerDataApiClient _apiClient;
    private readonly KycDbContext _dbContext;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<KycAggregationService> _logger;
    private const string CacheKeyPrefix = "kyc_data_";

    public KycAggregationService(
        ICustomerDataApiClient apiClient,
        KycDbContext dbContext,
        IMemoryCache memoryCache,
        ILogger<KycAggregationService> logger)
    {
        _apiClient = apiClient;
        _dbContext = dbContext;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<AggregatedKycData> GetAggregatedKycDataAsync(string ssn)
    {
        var cacheKey = $"{CacheKeyPrefix}{ssn}";

        // Try to get from in-memory cache first
        if (_memoryCache.TryGetValue(cacheKey, out AggregatedKycData? cachedData))
        {
            _logger.LogInformation("Retrieved KYC data from memory cache for SSN: {Ssn}", ssn);
            return cachedData!;
        }

        // Try to get from persistent database cache
        var persistedData = await _dbContext.CachedKycData
            .AsAsyncEnumerable()
            .FirstOrDefaultAsync(x => x.Ssn == ssn);

        if (persistedData != null)
        {
            _logger.LogInformation("Retrieved KYC data from persistent cache for SSN: {Ssn}", ssn);
            var aggregatedData = MapToAggregatedKycData(persistedData);
            
            // Restore to memory cache with expiration
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));
            _memoryCache.Set(cacheKey, aggregatedData, cacheOptions);
            
            return aggregatedData;
        }

        // Fetch from external API and cache
        _logger.LogInformation("Fetching fresh KYC data from external API for SSN: {Ssn}", ssn);
        var aggregatedKycData = await FetchAndAggregateAsync(ssn);

        // Persist to database
        await PersistCachedDataAsync(aggregatedKycData);

        // Store in memory cache
        var memCacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(1));
        _memoryCache.Set(cacheKey, aggregatedKycData, memCacheOptions);

        return aggregatedKycData;
    }

    private async Task<AggregatedKycData> FetchAndAggregateAsync(string ssn)
    {
        var personalDetails = await _apiClient.GetPersonalDetailsAsync(ssn);
        var contactDetails = await _apiClient.GetContactDetailsAsync(ssn);
        var kycForm = await _apiClient.GetKycFormAsync(ssn, DateTime.UtcNow);

        if (personalDetails == null || contactDetails == null || kycForm == null)
        {
            throw new InvalidOperationException($"Failed to retrieve complete KYC data for SSN: {ssn}");
        }

        var aggregatedData = new AggregatedKycData
        {
            Ssn = ssn,
            FirstName = personalDetails.first_name ?? string.Empty,
            LastName = personalDetails.sur_name ?? string.Empty,
            Address = FormatAddress(contactDetails.address),
            PhoneNumber = GetPreferredPhoneNumber(contactDetails.phone_numbers),
            Email = GetPreferredEmail(contactDetails.emails),
            TaxCountry = ExtractTaxCountryFromKycForm(kycForm),
            Income = ExtractIncomeFromKycForm(kycForm)
        };

        return aggregatedData;
    }

    private async Task PersistCachedDataAsync(AggregatedKycData data)
    {
        try
        {
            var existingRecord = await _dbContext.CachedKycData
                .AsAsyncEnumerable()
                .FirstOrDefaultAsync(x => x.Ssn == data.Ssn);

            if (existingRecord != null)
            {
                existingRecord.FirstName = data.FirstName;
                existingRecord.LastName = data.LastName;
                existingRecord.Address = data.Address;
                existingRecord.PhoneNumber = data.PhoneNumber;
                existingRecord.Email = data.Email;
                existingRecord.TaxCountry = data.TaxCountry;
                existingRecord.Income = data.Income;
                existingRecord.CachedAt = DateTime.UtcNow;
            }
            else
            {
                var cachedData = new CachedKycData
                {
                    Ssn = data.Ssn,
                    FirstName = data.FirstName,
                    LastName = data.LastName,
                    Address = data.Address,
                    PhoneNumber = data.PhoneNumber,
                    Email = data.Email,
                    TaxCountry = data.TaxCountry,
                    Income = data.Income,
                    CachedAt = DateTime.UtcNow
                };

                _dbContext.CachedKycData.Add(cachedData);
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Persisted KYC data cache for SSN: {Ssn}", data.Ssn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting KYC data cache for SSN: {Ssn}", data.Ssn);
            throw;
        }
    }

    private static string FormatAddress(Address[]? addresses)
    {
        if (addresses == null || addresses.Length == 0)
            return string.Empty;

        var address = addresses[0];
        var parts = new[] { address.street, address.postal_code, address.city, address.country }
            .Where(x => !string.IsNullOrEmpty(x))
            .ToList();

        return string.Join(", ", parts);
    }

    private static string? GetPreferredPhoneNumber(PhoneNumber[]? phoneNumbers)
    {
        if (phoneNumbers == null || phoneNumbers.Length == 0)
            return null;

        var preferred = phoneNumbers.FirstOrDefault(x => x.preferred);
        return preferred?.number ?? phoneNumbers.FirstOrDefault()?.number;
    }

    private static string? GetPreferredEmail(Email[]? emails)
    {
        if (emails == null || emails.Length == 0)
            return null;

        var preferred = emails.FirstOrDefault(x => x.preferred);
        return preferred?.email_address ?? emails.FirstOrDefault()?.email_address;
    }

    private static string ExtractTaxCountryFromKycForm(ExternalApis.Dtos.KycFormDto kycForm)
    {
        if (kycForm.items == null)
            return string.Empty;

        var taxCountryItem = kycForm.items.FirstOrDefault(x =>
            (x.key?.Equals("tax_country", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (x.Key?.Equals("tax_country", StringComparison.OrdinalIgnoreCase) ?? false));

        return taxCountryItem?.value ?? taxCountryItem?.Value ?? string.Empty;
    }

    private static int? ExtractIncomeFromKycForm(ExternalApis.Dtos.KycFormDto kycForm)
    {
        if (kycForm.items == null)
            return null;

        var incomeItem = kycForm.items.FirstOrDefault(x =>
            (x.key?.Equals("annual_income", StringComparison.OrdinalIgnoreCase) ?? false) ||
            (x.Key?.Equals("annual_income", StringComparison.OrdinalIgnoreCase) ?? false));

        var incomeValue = incomeItem?.value ?? incomeItem?.Value;
        if (incomeValue != null && int.TryParse(incomeValue, out var income))
        {
            return income;
        }

        return null;
    }

    private static AggregatedKycData MapToAggregatedKycData(CachedKycData cached)
    {
        return new AggregatedKycData
        {
            Ssn = cached.Ssn,
            FirstName = cached.FirstName,
            LastName = cached.LastName,
            Address = cached.Address,
            PhoneNumber = cached.PhoneNumber,
            Email = cached.Email,
            TaxCountry = cached.TaxCountry,
            Income = cached.Income
        };
    }
}

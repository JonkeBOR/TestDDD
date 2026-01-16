using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TestDDD.Data;
using TestDDD.ExternalApis;
using TestDDD.ExternalApis.Dtos;
using TestDDD.Models;
using TestDDD.Services;
using Xunit;

namespace TestDDD.Tests;

public class KycAggregationServiceTests
{
    private readonly ICustomerDataApiClient _mockApiClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<KycAggregationService> _mockLogger;

    public KycAggregationServiceTests()
    {
        _mockApiClient = Substitute.For<ICustomerDataApiClient>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockLogger = Substitute.For<ILogger<KycAggregationService>>();
    }

    private KycDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<KycDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new KycDbContext(options);
    }

    [Fact]
    public async Task GetAggregatedKycDataAsync_WithValidSsn_ReturnsAggregatedData()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = new KycAggregationService(_mockApiClient, dbContext, _memoryCache, _mockLogger);

        var ssn = "19800115-1234";
        var personalDetails = new PersonalDetailsDto
        {
            first_name = "Lars",
            sur_name = "Larsson"
        };
        var contactDetails = new ContactDetailsDto
        {
            address = new[]
            {
                new Address
                {
                    street = "Smågatan 1",
                    postal_code = "123 22",
                    city = "Malmö",
                    country = "Sweden"
                }
            },
            emails = new[]
            {
                new Email
                {
                    preferred = true,
                    email_address = "lars.larsson@example.com"
                }
            },
            phone_numbers = new[]
            {
                new PhoneNumber
                {
                    preferred = true,
                    number = "+46 70 123 45 67"
                }
            }
        };
        var kycForm = new KycFormDto
        {
            items = new[]
            {
                new KycItem { key = "tax_country", value = "SE" },
                new KycItem { key = "annual_income", value = "550000" }
                }
            };

            _mockApiClient.GetPersonalDetailsAsync(ssn).Returns(Task.FromResult(personalDetails)!);
            _mockApiClient.GetContactDetailsAsync(ssn).Returns(Task.FromResult(contactDetails)!);
            _mockApiClient.GetKycFormAsync(ssn, Arg.Any<DateTime>()).Returns(Task.FromResult(kycForm)!);

            // Act
            var result = await service.GetAggregatedKycDataAsync(ssn);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Lars", result.FirstName);
        Assert.Equal("Larsson", result.LastName);
        Assert.Equal("SE", result.TaxCountry);
        Assert.Equal(550000, result.Income);
        Assert.Equal("+46 70 123 45 67", result.PhoneNumber);
        Assert.Equal("lars.larsson@example.com", result.Email);
    }

    [Fact]
    public async Task GetAggregatedKycDataAsync_ReturnsCachedDataFromMemory_OnSecondCall()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = new KycAggregationService(_mockApiClient, dbContext, _memoryCache, _mockLogger);

        var ssn = "19800115-1234";
        var personalDetails = new PersonalDetailsDto { first_name = "Lars", sur_name = "Larsson" };
        var contactDetails = new ContactDetailsDto
        {
            address = new[] { new Address { street = "Smågatan 1", city = "Malmö" } },
            emails = new[] { new Email { email_address = "lars@example.com" } },
            phone_numbers = new[] { new PhoneNumber { number = "+46 70 123 45 67" } }
        };
        var kycForm = new KycFormDto
        {
            items = new[]
            {
                new KycItem { key = "tax_country", value = "SE" },
                new KycItem { key = "annual_income", value = "550000" }
                }
            };

            _mockApiClient.GetPersonalDetailsAsync(ssn).Returns(Task.FromResult(personalDetails)!);
            _mockApiClient.GetContactDetailsAsync(ssn).Returns(Task.FromResult(contactDetails)!);
            _mockApiClient.GetKycFormAsync(ssn, Arg.Any<DateTime>()).Returns(Task.FromResult(kycForm)!);

            // Act - First call
            await service.GetAggregatedKycDataAsync(ssn);

        // Act - Second call
        var result = await service.GetAggregatedKycDataAsync(ssn);

        // Assert
        Assert.NotNull(result);
        // API should not be called again on second request
        await _mockApiClient.Received(1).GetPersonalDetailsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetAggregatedKycDataAsync_ThrowsException_WhenApiReturnsNull()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = new KycAggregationService(_mockApiClient, dbContext, _memoryCache, _mockLogger);

        var ssn = "invalid-ssn";
        _mockApiClient.GetPersonalDetailsAsync(ssn).Returns(Task.FromResult((PersonalDetailsDto)null!));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAggregatedKycDataAsync(ssn));
    }

    [Fact]
    public async Task GetAggregatedKycDataAsync_HandlesNullContactDetails()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var service = new KycAggregationService(_mockApiClient, dbContext, _memoryCache, _mockLogger);

        var ssn = "19800115-1234";
        _mockApiClient.GetPersonalDetailsAsync(ssn)
            .Returns(Task.FromResult(new PersonalDetailsDto { first_name = "Lars", sur_name = "Larsson" }));
        _mockApiClient.GetContactDetailsAsync(ssn).Returns(Task.FromResult((ContactDetailsDto)null!));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAggregatedKycDataAsync(ssn));
    }
}

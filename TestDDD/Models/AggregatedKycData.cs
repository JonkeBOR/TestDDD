namespace TestDDD.Models;

/// <summary>
/// Represents aggregated KYC data response
/// </summary>
public class AggregatedKycData
{
    public string Ssn { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string TaxCountry { get; set; } = string.Empty;
    public int? Income { get; set; }
}

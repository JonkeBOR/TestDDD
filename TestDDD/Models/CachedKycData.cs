namespace TestDDD.Models;

/// <summary>
/// Database entity for persisting cached KYC data
/// </summary>
public class CachedKycData
{
    public int Id { get; set; }
    public string Ssn { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string TaxCountry { get; set; } = string.Empty;
    public int? Income { get; set; }
    public DateTime CachedAt { get; set; }
}

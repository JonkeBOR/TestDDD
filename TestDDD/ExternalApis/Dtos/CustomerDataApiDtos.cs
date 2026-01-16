namespace TestDDD.ExternalApis.Dtos;

/// <summary>
/// DTO for personal details from Customer Data API
/// </summary>
public class PersonalDetailsDto
{
    public string? first_name { get; set; }
    public string? sur_name { get; set; }
}

/// <summary>
/// DTO for contact details from Customer Data API
/// </summary>
public class ContactDetailsDto
{
    public Address[]? address { get; set; }
    public Email[]? emails { get; set; }
    public PhoneNumber[]? phone_numbers { get; set; }
}

public class Address
{
    public string? street { get; set; }
    public string? city { get; set; }
    public string? state { get; set; }
    public string? postal_code { get; set; }
    public string? country { get; set; }
}

public class Email
{
    public bool preferred { get; set; }
    public string? email_address { get; set; }
}

public class PhoneNumber
{
    public bool preferred { get; set; }
    public string? number { get; set; }
}

/// <summary>
/// DTO for KYC form data from Customer Data API
/// </summary>
public class KycFormDto
{
    public KycItem[]? items { get; set; }
}

public class KycItem
{
    public string? key { get; set; }
    public string? value { get; set; }
    public string? Key { get; set; }
    public string? Value { get; set; }
}

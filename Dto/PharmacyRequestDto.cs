
namespace PmfApi.Dto;

public record PharmacyRequest
{
    public required string Name {get;set;}

    public required string LicenseNumber {get;set;}

    public required int LocationId {get;set;}

    public int PhoneNumber {get;set;}

    public string? Email {get;set;}

    public bool IsVerified {get;set;} = true;

    public int FreshnessThreshold {get;set;}

}
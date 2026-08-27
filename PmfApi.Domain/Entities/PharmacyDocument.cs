namespace PmfApi.Domain.Entities;

public enum DocumentType
{
    License,BusinessRegistration,PharmacistCredential
}

public enum ReviewStatus
{
    Pending,Approved,Rejected
}

public class PharmacyDocument
{
    public int Id {get;set;}

    public int PharmacyId {get;set;}

    public required DocumentType DocumentType {get;set;}

    public required string FileUrl {get;set;}

    public DateTime UploadedAt {get;set;} = DateTime.UtcNow;

    public int? ReviewedByUserId {get;set;}

    public ReviewStatus ReviewStatus {get;set;} = ReviewStatus.Pending;

    public DateTime? ExpiresAt {get;set;}

    public Pharmacy Pharmacy {get;set;} = null!;
    public User? ReviewedByUser {get;set;}
}

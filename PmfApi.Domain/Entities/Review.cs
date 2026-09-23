namespace PmfApi.Domain.Entities;

// General service-experience feedback (staff, wait time, honored pricing) —
// kept deliberately separate from UserFeedback (inventory-accuracy feedback)
// so a bad-service review can't drag down the ReliabilityScore that
// inventory freshness/accuracy scoring depends on.
public class Review
{
    public int Id {get;set;}

    public int UserId {get;set;}

    public int PharmacyId {get;set;}

    public int Rating {get;set;}

    public string? Comment {get;set;}

    public DateTime SubmittedAt {get;set;} = DateTime.UtcNow;

    public User User {get;set;} = null!;
    public Pharmacy Pharmacy {get;set;} = null!;
}

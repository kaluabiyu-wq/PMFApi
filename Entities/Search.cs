namespace PmfApi.Entities;


public class Search
{
    public required int Id {get;init;}

    public required int UserId {get;init;}

    public required string MedicineSearch {get;init;}

    public int  LocationId {get;set;}

    public int ResultCount {get;init;}
    public DateTime SearchedAt {get;init;}

    public Location Location {get;set;} = null!;
}
namespace PmfApi.Entities;


public class Search
{
    public required int Id {get;init;}

    public required int UserId {get;init;}

    public required string MedicineSearch {get;init;}

    public decimal UserLatitude {get;init;}

    public decimal UserLongitude {get;init;}

    public int ResultCount {get;init;}
    public DateTime SearchedAt {get;init;}
}
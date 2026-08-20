
namespace PmfApi.Dto;

public class SearchRequest
{
    public required int UserId {get;set;}
    public required string MedicineSearch {get;set;}
    public required int LocationId {get;set;}

    public int ResultCount {get;set;}


}
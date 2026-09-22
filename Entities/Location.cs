
namespace PmfApi.Entities;


public class Location
{
    public int Id {get;set;}

    public required string Label {get;set;}

   public decimal Latitude {get;set;}

   public decimal Longitude {get;set;}

   public required string Subcity {get;set;}  

    public required string Woreda {get;set;}

    public string City {get;set;} = "Addis Ababa";
 public ICollection<Pharmacy> Inventories {get;set;} = new List<Pharmacy>();
 public ICollection<User> Users {get;set;} = new List<User>();


}
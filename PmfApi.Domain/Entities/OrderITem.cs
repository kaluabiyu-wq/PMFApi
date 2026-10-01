namespace PmfApi.Domain.Entities;

public class OrderItem
{
    public int Id {get; set;}
    public int OrderId {get;set;}
    public int InventoryId {get;set;}
    public int Quantitiy {get;set;}
    public decimal UintPrice {get;set;}

    public Order Order {get;set;} = null!;
    public Inventory Inventory {get;set;} = null!;
}
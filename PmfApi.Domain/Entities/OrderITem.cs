namespace PmfApi.Domain.Entities;

public class OrderItem
{
    public int Id {get; set;}
    public int OrderId {get;set;}
    public int InventoryId {get;set;}
    public int Quantity {get;set;}
    public decimal UnitPrice {get;set;}

    public Order Order {get;set;} = null!;
    public Inventory Inventory {get;set;} = null!;
}
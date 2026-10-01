namespace PmfApi.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
}

public class Order
{
    public int Id {get;set;}

    public int UserId {get;set;}
    public int PharmacyId {get;set;}
    public OrderStatus Status {get;set;} = OrderStatus.Pending;
    public DateTime CreatedAt {get;set;} = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User {get;set;} = null!;
    public Pharmacy Pharmacy {get;set;} = null!;
    public ICollection<OrderItem> Items {get;set;} = new List<OrderItem>();


    public static bool CanTransition(OrderStatus from, OrderStatus to,bool byPharmacy) =>
    (from, to) switch
    {
        (OrderStatus.Pending, OrderStatus.Confirmed) => byPharmacy,
        (OrderStatus.Pending, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed,OrderStatus.Cancelled) => byPharmacy,
        _ => false,
    };
}
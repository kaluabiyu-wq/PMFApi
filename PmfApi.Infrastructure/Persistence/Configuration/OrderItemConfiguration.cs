

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.HasKey(i => i.Id);

        b.HasIndex(i => new { i.OrderId, i.InventoryId }).IsUnique();
        b.HasIndex(i => i.InventoryId);

        b.HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(i => i.Inventory).WithMany().HasForeignKey(i => i.InventoryId).OnDelete(DeleteBehavior.Restrict);

        b.ToTable("OrderItems", t =>
            t.HasCheckConstraint("CK_OrderItem_Quantity_Positive", "\"Quantity\" > 0"));
    }
}
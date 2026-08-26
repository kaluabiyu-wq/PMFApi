

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;



namespace PmfApi.Infrastructure.Persistence.Configuration;


public class InventoriesConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> b)
    {
        b.HasKey(i => i.Id);
        b.HasIndex(i => new { i.MedicineId, i.PharmacyId}).IsUnique();
        b.HasOne(i => i.Medicine).WithMany(m => m.Inventories)
        .HasForeignKey(i => i.MedicineId);
        b.HasOne(i => i.Pharmacy).WithMany(p => p.Inventories)
        .HasForeignKey(i => i.PharmacyId);
    }
}
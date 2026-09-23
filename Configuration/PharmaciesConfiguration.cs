

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Entities;



namespace PmfApi.Configuration;

public class PharmaciesConfiguration : IEntityTypeConfiguration<Pharmacy>

{
    public void Configure(EntityTypeBuilder<Pharmacy> b)
    {
        b.HasKey(p => p.Id);
        b.Property(p => p.LicenceNumber).IsRequired().HasMaxLength(100);
        b.HasIndex(p => p.LicenceNumber).IsUnique();
        b.HasMany(p => p.Inventories).WithOne( p => p.Pharmacy)
        .HasForeignKey(p=>p.PharmacyId)
        .OnDelete(DeleteBehavior.Restrict);

    }

}


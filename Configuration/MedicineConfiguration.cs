

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Entities;


namespace PmfApi.Configuration;
public class MedicinesConfiguration : IEntityTypeConfiguration<Medicine>

{
    public void Configure(EntityTypeBuilder<Medicine> b)
    {
        b.HasKey(m => m.Id);
        b.Property(m => m.GenericName).IsRequired().HasMaxLength(200);
        b.Property(m => m.BrandName).IsRequired().HasMaxLength(200);
        b.HasMany(m => m.Inventories).WithOne( m => m.Medicine)
        .HasForeignKey(m =>m.MedicineId)
        .OnDelete(DeleteBehavior.Restrict);

    }

}


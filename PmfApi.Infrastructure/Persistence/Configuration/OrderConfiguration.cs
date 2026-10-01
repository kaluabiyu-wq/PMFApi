

using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder <Order> b)
    {
        b.HasKey( o => o.Id);
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasIndex(o => new {o.PharmacyId, o.Status, o.CreatedAt});
        b.HasIndex(o => new {o.UserId, o.CreatedAt});
        b.HasOne(o =>o.User).WithMany().HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(o =>o.Pharmacy).WithMany().HasForeignKey(o => o.PharmacyId).OnDelete(DeleteBehavior.Restrict);

        b.ToTable("Orders", t => t.
                 HasCheckConstraint("Ck_Order_Status", "\"Status\" IN ('Pending','Confirmed','Cancelled')"));

    }
}
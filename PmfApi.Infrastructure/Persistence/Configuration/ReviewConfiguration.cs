using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Configuration;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.HasKey(r => r.Id);

        b.Property(r => r.Rating).IsRequired();
        b.Property(r => r.Comment).HasMaxLength(2000);

        // Defense-in-depth: enforce the 1-5 range at the database level too,
        // not just via the [Range] attribute on the request DTO.
        b.ToTable(t => t.HasCheckConstraint("CK_Review_Rating_Range", "\"Rating\" >= 1 AND \"Rating\" <= 5"));

        b.HasIndex(r => new { r.PharmacyId, r.SubmittedAt });

        b.HasOne(r => r.User).WithMany()
        .HasForeignKey(r => r.UserId)
        .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(r => r.Pharmacy).WithMany(p => p.Reviews)
        .HasForeignKey(r => r.PharmacyId)
        .OnDelete(DeleteBehavior.Cascade);
    }
}

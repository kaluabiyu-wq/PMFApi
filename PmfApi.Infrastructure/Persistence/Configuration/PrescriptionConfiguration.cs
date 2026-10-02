using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> b)
    {
        b.HasKey(p => p.Id);

        b.Property(p => p.FileUrl).IsRequired().HasMaxLength(2048);
        b.Property(p => p.VerificationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(p => p.ReviewNote).HasMaxLength(500);

        
        b.HasIndex(p => p.OrderId).IsUnique().HasFilter("\"VerificationStatus\" = 'Pending'");
        b.HasIndex(p => p.VerifiedByUserId);

        
        b.HasOne(p => p.VerifiedByUser).WithMany().HasForeignKey(p => p.VerifiedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.ToTable("Prescriptions", t =>
        {
            t.HasCheckConstraint("CK_Prescription_Status",
                "\"VerificationStatus\" IN ('Pending', 'Approved', 'Rejected')");

            
            t.HasCheckConstraint("CK_Prescription_Verifier",
                "(\"VerificationStatus\" = 'Pending' AND \"VerifiedByUserId\" IS NULL AND \"VerifiedAt\" IS NULL) " +
                "OR (\"VerificationStatus\" <> 'Pending' AND \"VerifiedByUserId\" IS NOT NULL AND \"VerifiedAt\" IS NOT NULL)");
        });
    }
}
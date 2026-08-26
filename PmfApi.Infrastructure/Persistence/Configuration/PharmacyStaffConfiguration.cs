using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;

public class PharmacyStaffConfiguration : IEntityTypeConfiguration<PharmacyStaff>
{
    public void Configure(EntityTypeBuilder<PharmacyStaff> b)
    {
        b.HasKey(ps => ps.Id);

        b.Property(ps => ps.Position).HasMaxLength(100);

            b.HasIndex(ps => new { ps.UserId, ps.PharmacyId }).IsUnique();

        b.HasOne(ps => ps.User).WithMany()
        .HasForeignKey(ps => ps.UserId)
        .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(ps => ps.Pharmacy).WithMany(p => p.PharmacyStaff)
        .HasForeignKey(ps => ps.PharmacyId)
        .OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> b)
    {
        b.HasKey(a => a.Id);

        b.Property(a => a.EventType).HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(a => a.ReferenceTable).IsRequired().HasMaxLength(50);
        b.Property(a => a.Message).IsRequired().HasMaxLength(500);

        b.HasIndex(a => new { a.UserId, a.CreatedAt });   
        b.HasIndex(a => a.UserId).HasFilter("\"IsRead\" = false");

        b.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);

        b.ToTable("Alerts");
    }
}
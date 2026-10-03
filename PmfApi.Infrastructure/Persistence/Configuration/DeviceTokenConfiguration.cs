using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> b)
    {
        b.HasKey(d => d.Id);

        b.Property(d => d.Platform).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(d => d.PushToken).IsRequired().HasMaxLength(1024);
        b.HasIndex(d => d.PushToken).IsUnique();

        b.HasIndex(d => d.UserId);

        b.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId).OnDelete(DeleteBehavior.Cascade);

        b.ToTable("DeviceTokens", t =>
            t.HasCheckConstraint("CK_DeviceToken_Platform", "\"Platform\" IN ('Android', 'Ios', 'Web')"));
    }
}
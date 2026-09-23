using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Configuration;

public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> b)
    {
        b.HasKey(f => f.Id);

          b.HasIndex(f => new { f.UserId, f.PharmacyId }).IsUnique();

        b.HasOne(f => f.User).WithMany()
        .HasForeignKey(f => f.UserId)
        .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(f => f.Pharmacy).WithMany(p => p.Favorites)
        .HasForeignKey(f => f.PharmacyId)
        .OnDelete(DeleteBehavior.Cascade);
    }
}

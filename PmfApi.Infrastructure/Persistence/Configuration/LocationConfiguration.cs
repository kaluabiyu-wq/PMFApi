using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;


public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> b)
    {
        b.HasKey(l => l.Id);
        b.Property(l => l.Label).IsRequired().HasMaxLength(200);
        b.Property(l => l.City).IsRequired().HasMaxLength(200);

        b.OwnsOne(l => l.Coordinate, c =>
        {
            c.Property(x => x.Latitude).HasColumnName("Latitude");
            c.Property(x => x.Longitude).HasColumnName("Longitude");
            c.HasIndex(x => new { x.Latitude, x.Longitude }).IsUnique();
        });

        b.HasMany(l => l.Inventories).WithOne(p => p.Location)
            .HasForeignKey(p => p.LocationId);
    }
}
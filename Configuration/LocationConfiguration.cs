

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Entities;


namespace PmfApi.Configuration;
public class LocationConfiguration : IEntityTypeConfiguration<Location>

{
    public void Configure(EntityTypeBuilder<Location> b)
    {
        b.HasKey(l => l.Id);
        b.Property(l => l.Label).IsRequired().HasMaxLength(200);
        b.Property(l => l.City).IsRequired().HasMaxLength(200);
        b.HasIndex(l => new {l.Latitude,l.Longitude}).IsUnique();
        b.HasMany(l => l.Inventories).WithOne( l => l.Location);

    }

}


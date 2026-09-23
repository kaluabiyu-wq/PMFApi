

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;

public class RoleConfiguration : IEntityTypeConfiguration<Role>

{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.HasKey(r => r.Id);
        b.Property(r => r.Name).IsRequired().HasMaxLength(200);
        b.HasMany(u => u.Users).WithOne( r => r.Role)
        .HasForeignKey(r=>r.RoleId);
    }

}


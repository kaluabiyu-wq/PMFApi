

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;



namespace PmfApi.Configuration;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure (EntityTypeBuilder<User> b)
    {
        b.HasKey(u=>u.Id);
        b.Property(u=> u.FullName).HasMaxLength(200);
        b.Property(u=> u.Email).HasMaxLength(200);
        b.Property(u=> u.Password).HasMaxLength(200);
        b.HasIndex(u=>u.Email).IsUnique();
        
    }
}

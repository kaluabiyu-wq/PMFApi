using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(a => a.Id);
        b.Property(a => a.ActorName).HasMaxLength(200);
        b.Property(a => a.Action).HasMaxLength(50);
        b.Property(a => a.TargetType).HasMaxLength(50);
        b.Property(a => a.Detail).HasMaxLength(500);
        b.HasIndex(a => a.CreatedAt);
    }
}
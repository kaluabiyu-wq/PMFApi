using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Configuration;


public class PharmacyDocumentConfiguration : IEntityTypeConfiguration<PharmacyDocument>
{
    public void Configure(EntityTypeBuilder<PharmacyDocument> b)
    {
        b.HasKey(d => d.Id);

        b.Property(d => d.FileUrl).IsRequired();

        b.Property(d => d.DocumentType).HasConversion<string>().HasMaxLength(50);
        b.Property(d => d.ReviewStatus).HasConversion<string>().HasMaxLength(20);

       
    }
}

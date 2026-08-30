using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class FeeCategoryConfiguration : IEntityTypeConfiguration<FeeCategory>
{
    public void Configure(EntityTypeBuilder<FeeCategory> builder)
    {
        builder.ToTable("FeeCategories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => new { c.SchoolId, c.Name }).IsUnique();

        builder.HasOne(c => c.School)
            .WithMany()
            .HasForeignKey(c => c.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class StudentCategoryConfiguration : IEntityTypeConfiguration<StudentCategory>
{
    public void Configure(EntityTypeBuilder<StudentCategory> builder)
    {
        builder.ToTable("StudentCategories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.Name).IsUnique();
    }
}

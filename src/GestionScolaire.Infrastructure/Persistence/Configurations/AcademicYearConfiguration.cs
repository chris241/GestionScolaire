using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> builder)
    {
        builder.ToTable("AcademicYears");
        builder.HasKey(y => y.Id);

        builder.Property(y => y.Name).IsRequired().HasMaxLength(20);
        builder.HasIndex(y => y.Name).IsUnique();

        builder.HasMany(y => y.Terms)
            .WithOne(t => t.AcademicYear)
            .HasForeignKey(t => t.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(y => y.Classes)
            .WithOne(c => c.AcademicYear)
            .HasForeignKey(c => c.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

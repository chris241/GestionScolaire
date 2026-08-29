using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> builder)
    {
        builder.ToTable("AcademicPrograms");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Code).IsRequired().HasMaxLength(30);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.HasMany(p => p.Classes)
            .WithOne(c => c.Program)
            .HasForeignKey(c => c.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Courses)
            .WithOne(c => c.Program)
            .HasForeignKey(c => c.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

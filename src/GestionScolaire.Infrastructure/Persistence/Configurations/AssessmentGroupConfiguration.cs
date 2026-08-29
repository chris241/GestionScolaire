using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class AssessmentGroupConfiguration : IEntityTypeConfiguration<AssessmentGroup>
{
    public void Configure(EntityTypeBuilder<AssessmentGroup> builder)
    {
        builder.ToTable("AssessmentGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.Weightage).HasColumnType("decimal(5,2)");

        builder.HasOne(g => g.AcademicTerm)
            .WithMany()
            .HasForeignKey(g => g.AcademicTermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => new { g.AcademicTermId, g.Name }).IsUnique();
    }
}

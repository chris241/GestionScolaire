using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class StudentBatchConfiguration : IEntityTypeConfiguration<StudentBatch>
{
    public void Configure(EntityTypeBuilder<StudentBatch> builder)
    {
        builder.ToTable("StudentBatches");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(b => b.AcademicYear)
            .WithMany()
            .HasForeignKey(b => b.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.School)
            .WithMany()
            .HasForeignKey(b => b.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

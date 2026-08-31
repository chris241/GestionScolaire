using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class StudentFeeCategoryConfiguration : IEntityTypeConfiguration<StudentFeeCategory>
{
    public void Configure(EntityTypeBuilder<StudentFeeCategory> builder)
    {
        builder.ToTable("StudentFeeCategories");
        builder.HasKey(sfc => sfc.Id);

        builder.HasOne(sfc => sfc.Student)
            .WithMany()
            .HasForeignKey(sfc => sfc.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sfc => sfc.FeeCategory)
            .WithMany()
            .HasForeignKey(sfc => sfc.FeeCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sfc => new { sfc.StudentId, sfc.FeeCategoryId }).IsUnique();
    }
}

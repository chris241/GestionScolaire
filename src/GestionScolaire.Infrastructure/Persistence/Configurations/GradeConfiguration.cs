using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Score).HasColumnType("decimal(6,2)");
        builder.Property(g => g.MaxScore).HasColumnType("decimal(6,2)");
        builder.Property(g => g.Coefficient).HasColumnType("decimal(4,2)");
        builder.Property(g => g.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(g => g.Term).IsRequired().HasMaxLength(30);

        builder.HasOne(g => g.Subject)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Class)
            .WithMany(c => c.Grades)
            .HasForeignKey(g => g.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => new { g.StudentId, g.SubjectId, g.Term });
    }
}

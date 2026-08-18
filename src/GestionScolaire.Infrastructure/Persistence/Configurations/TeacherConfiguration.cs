using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> builder)
    {
        builder.ToTable("Teachers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Specialty).HasMaxLength(150);

        builder.HasMany(t => t.HomeroomClasses)
            .WithOne(c => c.HomeroomTeacher)
            .HasForeignKey(c => c.HomeroomTeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Grades)
            .WithOne(g => g.Teacher)
            .HasForeignKey(g => g.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

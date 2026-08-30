using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class TeacherSchoolConfiguration : IEntityTypeConfiguration<TeacherSchool>
{
    public void Configure(EntityTypeBuilder<TeacherSchool> builder)
    {
        builder.ToTable("TeacherSchools");
        builder.HasKey(ts => ts.Id);

        builder.HasOne(ts => ts.Teacher)
            .WithMany(t => t.Schools)
            .HasForeignKey(ts => ts.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ts => ts.School)
            .WithMany(s => s.Teachers)
            .HasForeignKey(ts => ts.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ts => new { ts.TeacherId, ts.SchoolId }).IsUnique();
    }
}

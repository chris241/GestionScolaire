using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("Guardians");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(g => g.LastName).IsRequired().HasMaxLength(100);
        builder.Property(g => g.Phone).IsRequired().HasMaxLength(30);
        builder.Property(g => g.Email).HasMaxLength(256);
        builder.Property(g => g.Occupation).HasMaxLength(100);
        builder.Property(g => g.AreasOfInterest).HasMaxLength(500);
    }
}

public class StudentGuardianConfiguration : IEntityTypeConfiguration<StudentGuardian>
{
    public void Configure(EntityTypeBuilder<StudentGuardian> builder)
    {
        builder.ToTable("StudentGuardians");
        builder.HasKey(sg => sg.Id);

        builder.Property(sg => sg.Relationship).IsRequired().HasMaxLength(50);

        builder.HasOne(sg => sg.Student)
            .WithMany()
            .HasForeignKey(sg => sg.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sg => sg.Guardian)
            .WithMany(g => g.Students)
            .HasForeignKey(sg => sg.GuardianId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sg => new { sg.StudentId, sg.GuardianId }).IsUnique();
    }
}

public class StudentSiblingConfiguration : IEntityTypeConfiguration<StudentSibling>
{
    public void Configure(EntityTypeBuilder<StudentSibling> builder)
    {
        builder.ToTable("StudentSiblings");
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SiblingStudent)
            .WithMany()
            .HasForeignKey(s => s.SiblingStudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.StudentId, s.SiblingStudentId }).IsUnique();
    }
}

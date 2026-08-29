using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class StudentGroupConfiguration : IEntityTypeConfiguration<StudentGroup>
{
    public void Configure(EntityTypeBuilder<StudentGroup> builder)
    {
        builder.ToTable("StudentGroups");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
        builder.Property(g => g.GroupType).IsRequired().HasMaxLength(50);

        builder.HasOne(g => g.AcademicYear)
            .WithMany()
            .HasForeignKey(g => g.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Class)
            .WithMany()
            .HasForeignKey(g => g.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Teacher)
            .WithMany()
            .HasForeignKey(g => g.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class StudentGroupMemberConfiguration : IEntityTypeConfiguration<StudentGroupMember>
{
    public void Configure(EntityTypeBuilder<StudentGroupMember> builder)
    {
        builder.ToTable("StudentGroupMembers");
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.StudentGroup)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.StudentGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Student)
            .WithMany(s => s.GroupMemberships)
            .HasForeignKey(m => m.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.StudentGroupId, m.StudentId }).IsUnique();
    }
}

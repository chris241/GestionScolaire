using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.EnrollmentNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.EnrollmentNumber).IsUnique();

        builder.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.LastName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Gender).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(s => s.Grades)
            .WithOne(g => g.Student)
            .HasForeignKey(g => g.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Attendances)
            .WithOne(a => a.Student)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Payments)
            .WithOne(p => p.Student)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.StudentCategory)
            .WithMany(c => c.Students)
            .HasForeignKey(s => s.StudentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.StudentBatch)
            .WithMany(b => b.Students)
            .HasForeignKey(s => s.StudentBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Logs)
            .WithOne(l => l.Student)
            .HasForeignKey(l => l.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StudentParentConfiguration : IEntityTypeConfiguration<StudentParent>
{
    public void Configure(EntityTypeBuilder<StudentParent> builder)
    {
        builder.ToTable("StudentParents");
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Relationship).HasMaxLength(50);

        builder.HasOne(sp => sp.Student)
            .WithMany(s => s.Parents)
            .HasForeignKey(sp => sp.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sp => sp.ParentUser)
            .WithMany(u => u.Children)
            .HasForeignKey(sp => sp.ParentUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(sp => new { sp.StudentId, sp.ParentUserId }).IsUnique();
    }
}

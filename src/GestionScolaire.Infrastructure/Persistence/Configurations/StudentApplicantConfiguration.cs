using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class StudentApplicantConfiguration : IEntityTypeConfiguration<StudentApplicant>
{
    public void Configure(EntityTypeBuilder<StudentApplicant> builder)
    {
        builder.ToTable("StudentApplicants");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.LastName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Gender).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.LevelAppliedFor).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(a => a.AcademicYear)
            .WithMany()
            .HasForeignKey(a => a.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ConvertedStudent)
            .WithMany()
            .HasForeignKey(a => a.ConvertedStudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Program)
            .WithMany()
            .HasForeignKey(a => a.ProgramId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.AdmissionCampaign)
            .WithMany(c => c.Applicants)
            .HasForeignKey(a => a.AdmissionCampaignId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.Status);
    }
}

using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class StudentLeaveApplicationConfiguration : IEntityTypeConfiguration<StudentLeaveApplication>
{
    public void Configure(EntityTypeBuilder<StudentLeaveApplication> builder)
    {
        builder.ToTable("StudentLeaveApplications");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Reason).IsRequired();
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => new { l.StudentId, l.Status });
    }
}

using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class TeacherLogConfiguration : IEntityTypeConfiguration<TeacherLog>
{
    public void Configure(EntityTypeBuilder<TeacherLog> builder)
    {
        builder.ToTable("TeacherLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.LogType).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Description).IsRequired();

        builder.HasOne(l => l.Teacher)
            .WithMany()
            .HasForeignKey(l => l.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.TeacherId, l.LogDate });
    }
}

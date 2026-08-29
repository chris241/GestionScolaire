using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class StudentLogConfiguration : IEntityTypeConfiguration<StudentLog>
{
    public void Configure(EntityTypeBuilder<StudentLog> builder)
    {
        builder.ToTable("StudentLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.LogType).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Description).IsRequired();

        builder.HasIndex(l => new { l.StudentId, l.LogDate });
    }
}

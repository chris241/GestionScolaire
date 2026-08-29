using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class EducationSettingsConfiguration : IEntityTypeConfiguration<EducationSettings>
{
    public void Configure(EntityTypeBuilder<EducationSettings> builder)
    {
        builder.ToTable("EducationSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SchoolName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Currency).IsRequired().HasMaxLength(10);
        builder.Property(s => s.DefaultMaxScore).HasColumnType("decimal(6,2)");
    }
}

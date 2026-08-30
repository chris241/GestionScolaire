using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class GradingScaleConfiguration : IEntityTypeConfiguration<GradingScale>
{
    public void Configure(EntityTypeBuilder<GradingScale> builder)
    {
        builder.ToTable("GradingScales");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(s => s.School)
            .WithMany()
            .HasForeignKey(s => s.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GradingScaleIntervalConfiguration : IEntityTypeConfiguration<GradingScaleInterval>
{
    public void Configure(EntityTypeBuilder<GradingScaleInterval> builder)
    {
        builder.ToTable("GradingScaleIntervals");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Grade).IsRequired().HasMaxLength(10);
        builder.Property(i => i.MinScore).HasColumnType("decimal(6,2)");
        builder.Property(i => i.MaxScore).HasColumnType("decimal(6,2)");

        builder.HasOne(i => i.GradingScale)
            .WithMany(s => s.Intervals)
            .HasForeignKey(i => i.GradingScaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

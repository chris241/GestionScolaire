using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class FeeStructureConfiguration : IEntityTypeConfiguration<FeeStructure>
{
    public void Configure(EntityTypeBuilder<FeeStructure> builder)
    {
        builder.ToTable("FeeStructures");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);

        builder.HasOne(s => s.AcademicYear)
            .WithMany()
            .HasForeignKey(s => s.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Program)
            .WithMany()
            .HasForeignKey(s => s.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FeeStructureItemConfiguration : IEntityTypeConfiguration<FeeStructureItem>
{
    public void Configure(EntityTypeBuilder<FeeStructureItem> builder)
    {
        builder.ToTable("FeeStructureItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount).HasColumnType("decimal(12,2)");

        builder.HasOne(i => i.FeeStructure)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.FeeStructureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.FeeCategory)
            .WithMany()
            .HasForeignKey(i => i.FeeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FeeScheduleConfiguration : IEntityTypeConfiguration<FeeSchedule>
{
    public void Configure(EntityTypeBuilder<FeeSchedule> builder)
    {
        builder.ToTable("FeeSchedules");
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.FeeStructure)
            .WithMany(f => f.Schedules)
            .HasForeignKey(s => s.FeeStructureId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.AcademicTerm)
            .WithMany()
            .HasForeignKey(s => s.AcademicTermId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

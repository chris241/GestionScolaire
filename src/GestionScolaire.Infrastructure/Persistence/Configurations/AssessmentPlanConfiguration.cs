using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class AssessmentPlanConfiguration : IEntityTypeConfiguration<AssessmentPlan>
{
    public void Configure(EntityTypeBuilder<AssessmentPlan> builder)
    {
        builder.ToTable("AssessmentPlans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.MaxScore).HasColumnType("decimal(6,2)");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.Course)
            .WithMany()
            .HasForeignKey(p => p.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Class)
            .WithMany()
            .HasForeignKey(p => p.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.AcademicTerm)
            .WithMany()
            .HasForeignKey(p => p.AcademicTermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.AssessmentGroup)
            .WithMany(g => g.Plans)
            .HasForeignKey(p => p.AssessmentGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.GradingScale)
            .WithMany()
            .HasForeignKey(p => p.GradingScaleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AssessmentCriteriaConfiguration : IEntityTypeConfiguration<AssessmentCriteria>
{
    public void Configure(EntityTypeBuilder<AssessmentCriteria> builder)
    {
        builder.ToTable("AssessmentCriteria");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.MaxScore).HasColumnType("decimal(6,2)");

        builder.HasOne(c => c.AssessmentPlan)
            .WithMany(p => p.Criteria)
            .HasForeignKey(c => c.AssessmentPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

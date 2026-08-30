using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class AdmissionCampaignConfiguration : IEntityTypeConfiguration<AdmissionCampaign>
{
    public void Configure(EntityTypeBuilder<AdmissionCampaign> builder)
    {
        builder.ToTable("AdmissionCampaigns");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);

        builder.HasOne(c => c.AcademicYear)
            .WithMany()
            .HasForeignKey(c => c.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AdmissionCampaignQuotaConfiguration : IEntityTypeConfiguration<AdmissionCampaignQuota>
{
    public void Configure(EntityTypeBuilder<AdmissionCampaignQuota> builder)
    {
        builder.ToTable("AdmissionCampaignQuotas");
        builder.HasKey(q => q.Id);

        builder.HasOne(q => q.AdmissionCampaign)
            .WithMany(c => c.Quotas)
            .HasForeignKey(q => q.AdmissionCampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.Program)
            .WithMany()
            .HasForeignKey(q => q.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(q => new { q.AdmissionCampaignId, q.ProgramId }).IsUnique();
    }
}

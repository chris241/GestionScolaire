using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.Property(i => i.TotalAmount).HasColumnType("decimal(12,2)");
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(i => i.Student)
            .WithMany()
            .HasForeignKey(i => i.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.FeeSchedule)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.FeeScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.FeeStructureItem)
            .WithMany()
            .HasForeignKey(i => i.FeeStructureItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.School)
            .WithMany()
            .HasForeignKey(i => i.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un élève a au plus une facture par catégorie et par échéance (un combiné n'existe plus : une
        // facture = un élève + une échéance + une catégorie).
        builder.HasIndex(i => new { i.StudentId, i.FeeScheduleId, i.FeeStructureItemId }).IsUnique();
    }
}

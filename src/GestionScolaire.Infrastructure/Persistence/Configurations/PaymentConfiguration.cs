using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Description).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Amount).HasColumnType("decimal(12,2)");
        builder.Property(p => p.AcademicYear).HasMaxLength(20);
        builder.Property(p => p.Term).HasMaxLength(30);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Method).HasMaxLength(50);
        builder.Property(p => p.InvoiceNumber).HasMaxLength(50);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.Status);
    }
}

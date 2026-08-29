using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("Classes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Level).IsRequired().HasMaxLength(50);

        builder.HasMany(c => c.Students)
            .WithOne(s => s.Class)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

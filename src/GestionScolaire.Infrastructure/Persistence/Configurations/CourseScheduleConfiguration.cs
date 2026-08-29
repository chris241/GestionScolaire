using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionScolaire.Infrastructure.Persistence.Configurations;

public class CourseScheduleConfiguration : IEntityTypeConfiguration<CourseSchedule>
{
    public void Configure(EntityTypeBuilder<CourseSchedule> builder)
    {
        builder.ToTable("CourseSchedules");
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.Course)
            .WithMany(c => c.Schedules)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Room)
            .WithMany(r => r.Schedules)
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Teacher)
            .WithMany()
            .HasForeignKey(s => s.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Class)
            .WithMany()
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.AcademicTerm)
            .WithMany()
            .HasForeignKey(s => s.AcademicTermId)
            .OnDelete(DeleteBehavior.Restrict);

        // Empêche de réserver deux fois la même salle sur le même créneau (même trimestre/jour/heure de début).
        builder.HasIndex(s => new { s.RoomId, s.AcademicTermId, s.DayOfWeek, s.StartTime }).IsUnique();
    }
}

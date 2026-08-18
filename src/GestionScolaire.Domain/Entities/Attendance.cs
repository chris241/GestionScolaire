using GestionScolaire.Domain.Common;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Domain.Entities;

public class Attendance : BaseEntity
{
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public Guid ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;

    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public AttendanceStatus Status { get; set; }
    public string? Comment { get; set; }

    public Guid RecordedByUserId { get; set; }
}

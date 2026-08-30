using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class TeacherLog : BaseEntity
{
    public Guid TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;

    public DateTime LogDate { get; set; } = DateTime.UtcNow;
    public string LogType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid RecordedByUserId { get; set; }

    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;
}

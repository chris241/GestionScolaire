using GestionScolaire.Domain.Common;

namespace GestionScolaire.Domain.Entities;

public class Room : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Building { get; set; }

    public ICollection<CourseSchedule> Schedules { get; set; } = new List<CourseSchedule>();
}

using GestionScolaire.Domain.Common;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Domain.Entities;

public class StudentLeaveApplication : BaseEntity
{
    public Guid SchoolId { get; set; }
    public School School { get; set; } = null!;

    public Guid StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;

    public LeaveApplicationStatus Status { get; set; } = LeaveApplicationStatus.Pending;
    public Guid RequestedByUserId { get; set; }

    public DateTime? DecisionDate { get; set; }
    public string? DecisionNotes { get; set; }
}

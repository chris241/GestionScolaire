using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.LeaveApplications;

public record LeaveApplicationDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    DateTime StartDate,
    DateTime EndDate,
    string Reason,
    string Status,
    DateTime? DecisionDate,
    string? DecisionNotes
);

public record CreateLeaveApplicationRequest(
    [Required] Guid StudentId,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    [Required] string Reason
);

public record DecideLeaveApplicationRequest(
    [Required] bool Approve,
    string? DecisionNotes
);

using System.ComponentModel.DataAnnotations;
using GestionScolaire.Domain.Enums;

namespace GestionScolaire.Application.DTOs.Attendances;

public record AttendanceDto(
    Guid? Id,
    Guid StudentId,
    string StudentFullName,
    Guid ClassId,
    DateTime Date,
    string? Status,
    string? Comment
);

public record AttendanceEntryRequest(
    [Required] Guid StudentId,
    [Required] AttendanceStatus Status,
    string? Comment
);

public record BulkMarkAttendanceRequest(
    [Required] Guid ClassId,
    [Required] DateTime Date,
    [Required] List<AttendanceEntryRequest> Entries
);

public record AbsentStudentDto(
    Guid StudentId,
    string StudentFullName,
    Guid ClassId,
    string ClassName,
    string Status,
    string? Comment
);

/// DayStatuses associe le jour du mois (1-31) au statut du jour ; les jours sans enregistrement sont absents du dictionnaire.
public record MonthlyAttendanceRowDto(
    Guid StudentId,
    string StudentFullName,
    Dictionary<int, string> DayStatuses
);

public record StudentAttendanceSummaryDto(
    Guid StudentId,
    string StudentFullName,
    int PresentCount,
    int AbsentCount,
    int RetardCount,
    int ExcuseCount,
    int TotalRecorded
);

public record BatchAttendanceSummaryDto(
    Guid BatchId,
    string BatchName,
    List<StudentAttendanceSummaryDto> Students
);

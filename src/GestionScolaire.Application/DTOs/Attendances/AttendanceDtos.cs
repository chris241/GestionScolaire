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

using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.StudentLogs;

public record StudentLogDto(
    Guid Id,
    Guid StudentId,
    DateTime LogDate,
    string LogType,
    string Description
);

public record CreateStudentLogRequest(
    [Required] Guid StudentId,
    [Required] DateTime LogDate,
    [Required] string LogType,
    [Required] string Description
);

public record UpdateStudentLogRequest(
    [Required] DateTime LogDate,
    [Required] string LogType,
    [Required] string Description
);

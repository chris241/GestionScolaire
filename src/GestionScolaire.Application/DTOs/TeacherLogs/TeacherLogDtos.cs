using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.TeacherLogs;

public record TeacherLogDto(
    Guid Id,
    Guid TeacherId,
    DateTime LogDate,
    string LogType,
    string Description
);

public record CreateTeacherLogRequest(
    [Required] Guid TeacherId,
    [Required] DateTime LogDate,
    [Required] string LogType,
    [Required] string Description
);

public record UpdateTeacherLogRequest(
    [Required] DateTime LogDate,
    [Required] string LogType,
    [Required] string Description
);

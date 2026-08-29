using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.CourseSchedules;

public record CourseScheduleDto(
    Guid Id,
    Guid CourseId,
    string CourseName,
    Guid RoomId,
    string RoomName,
    Guid TeacherId,
    string TeacherName,
    Guid? ClassId,
    string? ClassName,
    Guid AcademicTermId,
    string AcademicTermName,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime
);

public record CreateCourseScheduleRequest(
    [Required] Guid CourseId,
    [Required] Guid RoomId,
    [Required] Guid TeacherId,
    Guid? ClassId,
    [Required] Guid AcademicTermId,
    [Required] DayOfWeek DayOfWeek,
    [Required] TimeOnly StartTime,
    [Required] TimeOnly EndTime
);

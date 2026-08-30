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

/// Une matière à placer par l'assistant : combien de séances par semaine, avec quel enseignant.
public record ScheduleRequirementInput(
    [Required] Guid CourseId,
    [Required] Guid TeacherId,
    [Range(1, 10)] int SessionsPerWeek
);

public record AutoPlanScheduleRequest(
    [Required] Guid ClassId,
    [Required] Guid AcademicTermId,
    [Required, MinLength(1)] List<DayOfWeek> Days,
    [Required] TimeOnly DailyStartTime,
    [Range(1, 12)] int PeriodsPerDay,
    [Range(15, 180)] int PeriodDurationMinutes,
    [Required, MinLength(1)] List<ScheduleRequirementInput> Requirements
);

public record ProposedScheduleSlot(
    Guid CourseId,
    string CourseName,
    Guid TeacherId,
    string TeacherName,
    Guid RoomId,
    string RoomName,
    Guid ClassId,
    Guid AcademicTermId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime
);

public record AutoPlanScheduleResultDto(
    bool FullyPlaced,
    List<ProposedScheduleSlot> Proposed,
    List<string> Unplaced
);

public record CommitAutoPlanRequest([Required, MinLength(1)] List<ProposedScheduleSlot> Slots);

public record CommitAutoPlanResultDto(
    List<CourseScheduleDto> Created,
    List<string> Skipped
);

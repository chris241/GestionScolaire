using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.CourseEnrollments;

public record CourseEnrollmentDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    Guid CourseId,
    string CourseName,
    Guid AcademicYearId,
    string AcademicYearName,
    DateTime EnrollmentDate,
    string Status
);

public record CreateCourseEnrollmentRequest(
    [Required] Guid StudentId,
    [Required] Guid CourseId,
    [Required] Guid AcademicYearId
);

public record BulkCourseEnrollRequest(
    [Required] List<Guid> StudentIds,
    [Required] Guid CourseId,
    [Required] Guid AcademicYearId
);

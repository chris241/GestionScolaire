using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.StudentGroups;

public record StudentGroupDto(
    Guid Id,
    string Name,
    string GroupType,
    int? MaxSize,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid? ClassId,
    string? ClassName,
    Guid? TeacherId,
    string? TeacherName,
    int MemberCount
);

public record StudentGroupMemberDto(Guid Id, Guid StudentId, string StudentFullName);

public record CreateStudentGroupRequest(
    [Required] string Name,
    [Required] string GroupType,
    int? MaxSize,
    [Required] Guid AcademicYearId,
    Guid? ClassId,
    Guid? TeacherId
);

public record UpdateStudentGroupRequest(
    [Required] string Name,
    [Required] string GroupType,
    int? MaxSize,
    Guid? ClassId,
    Guid? TeacherId
);

public record AddGroupMembersRequest([Required] List<Guid> StudentIds);

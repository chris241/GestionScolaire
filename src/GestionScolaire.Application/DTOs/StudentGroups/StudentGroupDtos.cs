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
    int MemberCount
);

public record StudentGroupMemberDto(Guid Id, Guid StudentId, string StudentFullName);

public record CreateStudentGroupRequest(
    [Required] string Name,
    [Required] string GroupType,
    int? MaxSize,
    [Required] Guid AcademicYearId,
    Guid? ClassId
);

public record UpdateStudentGroupRequest(
    [Required] string Name,
    [Required] string GroupType,
    int? MaxSize,
    Guid? ClassId
);

public record AddGroupMembersRequest([Required] List<Guid> StudentIds);

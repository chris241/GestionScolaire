using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.Courses;

public record CourseDto(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    Guid SubjectId,
    string SubjectName,
    Guid ProgramId,
    string ProgramName,
    List<TopicDto> Topics
);

public record TopicDto(Guid Id, string Name, string? Description, string? Content, int Order);

public record CreateCourseRequest(
    [Required] string Name,
    string? Code,
    string? Description,
    [Required] Guid SubjectId,
    [Required] Guid ProgramId
);

public record UpdateCourseRequest(
    [Required] string Name,
    string? Code,
    string? Description
);

public record CreateTopicRequest(
    [Required] string Name,
    string? Description,
    string? Content,
    int Order
);

public record UpdateTopicRequest(
    [Required] string Name,
    string? Description,
    string? Content,
    int Order
);

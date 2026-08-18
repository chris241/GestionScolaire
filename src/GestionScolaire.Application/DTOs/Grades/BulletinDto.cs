namespace GestionScolaire.Application.DTOs.Grades;

public class BulletinSubjectLineDto
{
    public string SubjectName { get; set; } = string.Empty;
    public decimal Coefficient { get; set; }
    public decimal Average { get; set; }
    public string? TeacherName { get; set; }
    public string? Appreciation { get; set; }
}

public class BulletinDto
{
    public string SchoolName { get; set; } = "Établissement Scolaire";
    public string StudentFullName { get; set; } = string.Empty;
    public string EnrollmentNumber { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string AcademicYear { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public List<BulletinSubjectLineDto> Subjects { get; set; } = new();

    public decimal GeneralAverage { get; set; }
    public int ClassRank { get; set; }
    public int ClassSize { get; set; }
    public string Mention { get; set; } = string.Empty;
}

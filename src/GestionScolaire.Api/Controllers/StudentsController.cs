using System.Globalization;
using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.Students;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public StudentsController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet]
    public async Task<ActionResult<List<StudentDto>>> GetAll([FromQuery] Guid? classId)
    {
        var query = _context.Students.Include(s => s.Class).AsQueryable();

        if (classId.HasValue)
            query = query.Where(s => s.ClassId == classId.Value);

        if (_currentUser.Role == nameof(UserRole.Parent))
        {
            // Un Parent n'a pas de claim école (voir AuthController) : son accès reste scopé élève par
            // élève via StudentParent, donc on ignore le filtre multi-établissement pour cette branche.
            query = query.IgnoreQueryFilters();

            var childIds = _context.StudentParents
                .Where(sp => sp.ParentUserId == _currentUser.UserId)
                .Select(sp => sp.StudentId);

            query = query.Where(s => childIds.Contains(s.Id));
        }
        else if (_currentUser.Role == nameof(UserRole.Teacher))
        {
            // MVP : un professeur n'est titulaire (HomeroomTeacher) que d'une seule classe.
            var teacherClassIds = _context.Classes
                .Where(c => c.HomeroomTeacher != null && c.HomeroomTeacher.UserId == _currentUser.UserId)
                .Select(c => c.Id);

            query = query.Where(s => teacherClassIds.Contains(s.ClassId));
        }
        else if (_currentUser.Role == nameof(UserRole.Student))
        {
            // Portail élève : un élève ne voit que son propre dossier.
            query = query.Where(s => s.UserId == _currentUser.UserId);
        }

        var students = await query
            .OrderBy(s => s.LastName)
            .Select(s => new StudentDto(
                s.Id, s.EnrollmentNumber, s.FirstName, s.LastName,
                s.DateOfBirth, s.Gender.ToString(), s.ClassId, s.Class.Name, s.IsActive))
            .ToListAsync();

        return Ok(students);
    }

    [HttpGet("{studentId:guid}/siblings")]
    public async Task<ActionResult<List<SiblingDto>>> GetSiblings(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        var siblingsQuery = _context.StudentSiblings
            .Include(s => s.Student).ThenInclude(s => s.Class)
            .Include(s => s.SiblingStudent).ThenInclude(s => s.Class)
            .Where(s => s.StudentId == studentId || s.SiblingStudentId == studentId);

        // Un Parent (déjà vérifié ci-dessus via l'access policy) n'a pas de claim école. Pour tout autre
        // rôle le filtre reste actif : HasAccessAsync ne vérifie pas l'école pour un Directeur, ce sont
        // les filtres Student (via Class) sur les deux navigations qui referment la frontière ici.
        if (_currentUser.Role == nameof(UserRole.Parent))
            siblingsQuery = siblingsQuery.IgnoreQueryFilters();

        var links = await siblingsQuery.ToListAsync();

        var siblings = links
            .Select(l => l.StudentId == studentId ? l.SiblingStudent : l.Student)
            .Select(s => new SiblingDto(s.Id, s.FullName, s.EnrollmentNumber, s.Class.Name))
            .ToList();

        return Ok(siblings);
    }

    [HttpPost("{studentId:guid}/siblings/{siblingStudentId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> AddSibling(Guid studentId, Guid siblingStudentId)
    {
        if (studentId == siblingStudentId)
            return BadRequest(new { message = "Un élève ne peut pas être son propre frère/sœur." });

        var student = await _context.Students.FindAsync(studentId);
        var sibling = await _context.Students.FindAsync(siblingStudentId);
        if (student is null || sibling is null) return NotFound(new { message = "Élève introuvable." });

        var alreadyLinked = await _context.StudentSiblings.AnyAsync(s =>
            (s.StudentId == studentId && s.SiblingStudentId == siblingStudentId) ||
            (s.StudentId == siblingStudentId && s.SiblingStudentId == studentId));
        if (alreadyLinked) return Conflict(new { message = "Ce lien de fratrie existe déjà." });

        _context.StudentSiblings.Add(new StudentSibling { StudentId = studentId, SiblingStudentId = siblingStudentId });
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{studentId:guid}/siblings/{siblingStudentId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> RemoveSibling(Guid studentId, Guid siblingStudentId)
    {
        var link = await _context.StudentSiblings.FirstOrDefaultAsync(s =>
            (s.StudentId == studentId && s.SiblingStudentId == siblingStudentId) ||
            (s.StudentId == siblingStudentId && s.SiblingStudentId == studentId));
        if (link is null) return NotFound();

        _context.StudentSiblings.Remove(link);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// Année académique de la classe de cet élève (bornes de dates), pour construire un calendrier des
    /// mois de l'année scolaire côté Parent — qui n'a pas de claim école et ne peut pas appeler
    /// /api/academicyears directement.
    [HttpGet("{studentId:guid}/academic-year")]
    public async Task<ActionResult<AcademicYearDto>> GetAcademicYear(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        var isParent = _currentUser.Role == nameof(UserRole.Parent);
        var studentQuery = _context.Students.Include(s => s.Class).ThenInclude(c => c.AcademicYear).Where(s => s.Id == studentId);
        if (isParent) studentQuery = studentQuery.IgnoreQueryFilters();

        var student = await studentQuery.FirstOrDefaultAsync();
        if (student is null) return NotFound();

        var year = student.Class.AcademicYear;
        return Ok(new AcademicYearDto(year.Id, year.Name, year.StartDate, year.EndDate, year.IsCurrent));
    }

    /// Liste toutes les catégories de frais de l'école active avec, pour chacune, si cet élève y est
    /// assujetti (obligatoire pour tous, ou abonnement explicite pour une catégorie optionnelle comme
    /// Cantine/Transport).
    [HttpGet("{studentId:guid}/fee-categories")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<StudentFeeCategoryDto>>> GetFeeCategories(Guid studentId)
    {
        if (await _context.Students.FindAsync(studentId) is null) return NotFound();

        var categories = await _context.FeeCategories.OrderBy(c => c.Name).ToListAsync();
        var subscribedIds = await _context.StudentFeeCategories
            .Where(sfc => sfc.StudentId == studentId)
            .Select(sfc => sfc.FeeCategoryId)
            .ToListAsync();

        var result = categories.Select(c => new StudentFeeCategoryDto(
            c.Id, c.Name, c.IsMandatory, c.IsMandatory || subscribedIds.Contains(c.Id)));

        return Ok(result);
    }

    [HttpPost("{studentId:guid}/fee-categories/{categoryId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> SubscribeToFeeCategory(Guid studentId, Guid categoryId)
    {
        var student = await _context.Students.FindAsync(studentId);
        var category = await _context.FeeCategories.FindAsync(categoryId);
        if (student is null || category is null) return NotFound(new { message = "Élève ou catégorie de frais introuvable." });

        if (category.IsMandatory)
            return BadRequest(new { message = "Cette catégorie est obligatoire : tous les élèves y sont déjà assujettis." });

        var alreadySubscribed = await _context.StudentFeeCategories
            .AnyAsync(sfc => sfc.StudentId == studentId && sfc.FeeCategoryId == categoryId);
        if (alreadySubscribed) return Conflict(new { message = "Cet élève est déjà abonné à cette catégorie." });

        _context.StudentFeeCategories.Add(new StudentFeeCategory { StudentId = studentId, FeeCategoryId = categoryId });
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{studentId:guid}/fee-categories/{categoryId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> UnsubscribeFromFeeCategory(Guid studentId, Guid categoryId)
    {
        var link = await _context.StudentFeeCategories
            .FirstOrDefaultAsync(sfc => sfc.StudentId == studentId && sfc.FeeCategoryId == categoryId);
        if (link is null) return NotFound();

        _context.StudentFeeCategories.Remove(link);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// Outil de création en masse : un fichier CSV (en-tête ignoré) avec les colonnes
    /// FirstName,LastName,DateOfBirth(AAAA-MM-JJ),Gender(Masculin/Feminin),ClassName,EnrollmentNumber(optionnel).
    /// Chaque ligne est validée indépendamment ; les lignes valides sont importées même si d'autres échouent.
    [HttpPost("import")]
    [Authorize(Roles = "Director")]
    [RequestSizeLimit(2_000_000)]
    public async Task<ActionResult<StudentImportResultDto>> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier fourni." });

        var classIdsByName = await _context.Classes.ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
        var usedEnrollmentNumbers = new HashSet<string>(
            await _context.Students.Select(s => s.EnrollmentNumber).ToListAsync(),
            StringComparer.OrdinalIgnoreCase);
        var nextSequentialNumber = usedEnrollmentNumbers.Count + 1;

        var rows = new List<StudentImportRowResult>();
        var toAdd = new List<Student>();

        using var reader = new StreamReader(file.OpenReadStream());
        await reader.ReadLineAsync(); // en-tête ignoré

        var rowNumber = 1;
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = ParseCsvLine(line);
            var firstName = fields.ElementAtOrDefault(0)?.Trim() ?? "";
            var lastName = fields.ElementAtOrDefault(1)?.Trim() ?? "";

            if (fields.Length < 5)
            {
                rows.Add(new StudentImportRowResult(rowNumber, false, firstName, lastName, null, "Ligne incomplète (5 colonnes minimum attendues)."));
                continue;
            }

            var dateOfBirthRaw = fields[2].Trim();
            var genderRaw = fields[3].Trim();
            var className = fields[4].Trim();
            var enrollmentNumberRaw = fields.Length > 5 ? fields[5].Trim() : "";

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                rows.Add(new StudentImportRowResult(rowNumber, false, firstName, lastName, null, "Prénom et nom requis."));
                continue;
            }

            if (!DateTime.TryParse(dateOfBirthRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOfBirth))
            {
                rows.Add(new StudentImportRowResult(rowNumber, false, firstName, lastName, null, $"Date de naissance invalide : « {dateOfBirthRaw} » (format attendu AAAA-MM-JJ)."));
                continue;
            }

            if (!Enum.TryParse<Gender>(genderRaw, true, out var gender))
            {
                rows.Add(new StudentImportRowResult(rowNumber, false, firstName, lastName, null, $"Genre invalide : « {genderRaw} » (Masculin ou Feminin attendu)."));
                continue;
            }

            if (!classIdsByName.TryGetValue(className, out var classId))
            {
                rows.Add(new StudentImportRowResult(rowNumber, false, firstName, lastName, null, $"Classe introuvable : « {className} »."));
                continue;
            }

            string enrollmentNumber;
            if (!string.IsNullOrWhiteSpace(enrollmentNumberRaw))
            {
                if (!usedEnrollmentNumbers.Add(enrollmentNumberRaw))
                {
                    rows.Add(new StudentImportRowResult(rowNumber, false, firstName, lastName, null, $"Matricule déjà utilisé : « {enrollmentNumberRaw} »."));
                    continue;
                }
                enrollmentNumber = enrollmentNumberRaw;
            }
            else
            {
                do
                {
                    enrollmentNumber = $"MAT-{DateTime.UtcNow.Year}-{nextSequentialNumber:000}";
                    nextSequentialNumber++;
                } while (!usedEnrollmentNumbers.Add(enrollmentNumber));
            }

            var student = new Student
            {
                EnrollmentNumber = enrollmentNumber,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = dateOfBirth.AsUtc(),
                Gender = gender,
                EnrollmentDate = DateTime.UtcNow,
                ClassId = classId
            };

            toAdd.Add(student);
            rows.Add(new StudentImportRowResult(rowNumber, true, firstName, lastName, student.Id, null));
        }

        if (toAdd.Count > 0)
        {
            _context.Students.AddRange(toAdd);
            await _context.SaveChangesAsync();
        }

        return Ok(new StudentImportResultDto(rows.Count, rows.Count(r => r.Success), rows.Count(r => !r.Success), rows));
    }

    /// Parseur CSV minimal : gère les champs entre guillemets (avec virgules ou guillemets doublés à l'intérieur).
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else if (c == '"') inQuotes = false;
                else current.Append(c);
            }
            else if (c == '"') inQuotes = true;
            else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        fields.Add(current.ToString());

        return fields.ToArray();
    }

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }
}

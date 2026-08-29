using GestionScolaire.Application.Common.Interfaces;
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

        var links = await _context.StudentSiblings
            .Include(s => s.Student).ThenInclude(s => s.Class)
            .Include(s => s.SiblingStudent).ThenInclude(s => s.Class)
            .Where(s => s.StudentId == studentId || s.SiblingStudentId == studentId)
            .ToListAsync();

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

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }
}

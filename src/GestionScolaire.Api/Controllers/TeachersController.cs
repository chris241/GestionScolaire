using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Teachers;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using GestionScolaire.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeachersController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public TeachersController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeacherDto>>> GetAll()
    {
        var teachers = await BaseQuery().OrderBy(t => t.User.LastName).ToListAsync();
        return Ok(teachers.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<TeacherDto>> Create(CreateTeacherRequest request)
    {
        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Conflict(new { message = "Un compte existe déjà avec cet email." });

        var user = new User
        {
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Teacher
        };
        _context.Users.Add(user);

        var teacher = new Teacher
        {
            User = user,
            Specialty = request.Specialty,
            HireDate = request.HireDate.AsUtc()
        };
        _context.Teachers.Add(teacher);

        // Rattache l'enseignant à l'école active du Directeur : sans ce lien TeacherSchool, l'enseignant
        // serait immédiatement invisible (filtré partout, y compris dans GetAll ci-dessus) et ne pourrait
        // jamais se connecter (aucune école accessible à la claim JWT).
        _context.TeacherSchools.Add(new TeacherSchool { Teacher = teacher, SchoolId = _currentUser.SchoolId!.Value });

        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(t => t.Id == teacher.Id);
        return Ok(ToDto(full));
    }

    /// Rattache un enseignant déjà existant à une autre école du même Directeur — le mécanisme qui permet
    /// à un enseignant de couvrir plusieurs établissements (ex. professeur de langue partagé entre deux
    /// écoles). L'enseignant doit déjà être visible dans l'école active (donc appartenir au même
    /// Directeur) ; l'école cible est validée comme appartenant elle aussi au Directeur courant.
    [HttpPost("{id:guid}/schools/{schoolId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> LinkToSchool(Guid id, Guid schoolId)
    {
        var teacher = await _context.Teachers.FindAsync(id);
        if (teacher is null) return NotFound(new { message = "Enseignant introuvable." });

        var school = await _context.Schools.FirstOrDefaultAsync(s => s.Id == schoolId && s.DirectorId == _currentUser.UserId);
        if (school is null) return NotFound(new { message = "École introuvable." });

        var alreadyLinked = await _context.TeacherSchools.AnyAsync(ts => ts.TeacherId == id && ts.SchoolId == schoolId);
        if (alreadyLinked) return Conflict(new { message = "Cet enseignant est déjà rattaché à cette école." });

        _context.TeacherSchools.Add(new TeacherSchool { TeacherId = id, SchoolId = schoolId });
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(t => t.Id == id);
        return Ok(ToDto(full));
    }

    /// Détache un enseignant d'une école. Toujours refusé s'il ne lui reste plus qu'une seule école
    /// rattachée : un enseignant sans aucune école serait immédiatement invisible et ne pourrait plus se
    /// connecter (même piège que la création sans lien, corrigé en Phase 8 de la migration multi-école).
    [HttpDelete("{id:guid}/schools/{schoolId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> UnlinkFromSchool(Guid id, Guid schoolId)
    {
        var link = await _context.TeacherSchools.FirstOrDefaultAsync(ts => ts.TeacherId == id && ts.SchoolId == schoolId);
        if (link is null) return NotFound();

        var linkCount = await _context.TeacherSchools.CountAsync(ts => ts.TeacherId == id);
        if (linkCount <= 1)
            return BadRequest(new { message = "Impossible de retirer la dernière école d'un enseignant." });

        _context.TeacherSchools.Remove(link);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private IQueryable<Teacher> BaseQuery() => _context.Teachers
        .Include(t => t.User)
        .Include(t => t.Schools).ThenInclude(ts => ts.School);

    private static TeacherDto ToDto(Teacher t) => new(
        t.Id, $"{t.User.FirstName} {t.User.LastName}", t.Specialty, t.User.Email, t.HireDate,
        t.Schools.Select(ts => new TeacherSchoolSummaryDto(ts.SchoolId, ts.School.Name)).OrderBy(s => s.Name).ToList());
}

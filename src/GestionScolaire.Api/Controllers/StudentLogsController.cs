using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.StudentLogs;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentLogsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public StudentLogsController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<StudentLogDto>>> GetByStudent(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        var query = _context.StudentLogs.Where(l => l.StudentId == studentId);

        // Un Parent (déjà vérifié ci-dessus via l'access policy) n'a pas de claim école. Pour tout autre
        // rôle le filtre reste actif : HasAccessAsync ne vérifie pas l'école pour un Directeur, c'est le
        // filtre StudentLog (colonne SchoolId propre) qui referme la frontière multi-tenant ici.
        if (_currentUser.Role == nameof(UserRole.Parent))
            query = query.IgnoreQueryFilters();

        var logs = await query
            .OrderByDescending(l => l.LogDate)
            .Select(l => new StudentLogDto(l.Id, l.StudentId, l.LogDate, l.LogType, l.Description))
            .ToListAsync();

        return Ok(logs);
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Director")]
    public async Task<ActionResult<StudentLogDto>> Create(CreateStudentLogRequest request)
    {
        if (!await HasAccessAsync(request.StudentId)) return Forbid();

        var student = await _context.Students.FindAsync(request.StudentId);
        if (student is null) return NotFound(new { message = "Élève introuvable." });

        var log = new StudentLog
        {
            StudentId = request.StudentId,
            LogDate = request.LogDate.AsUtc(),
            LogType = request.LogType,
            Description = request.Description,
            RecordedByUserId = _currentUser.UserId!.Value,
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.StudentLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new StudentLogDto(log.Id, log.StudentId, log.LogDate, log.LogType, log.Description));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Teacher,Director")]
    public async Task<ActionResult<StudentLogDto>> Update(Guid id, UpdateStudentLogRequest request)
    {
        var log = await _context.StudentLogs.FindAsync(id);
        if (log is null) return NotFound();
        if (!await HasAccessAsync(log.StudentId)) return Forbid();

        log.LogDate = request.LogDate.AsUtc();
        log.LogType = request.LogType;
        log.Description = request.Description;
        await _context.SaveChangesAsync();

        return Ok(new StudentLogDto(log.Id, log.StudentId, log.LogDate, log.LogType, log.Description));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Teacher,Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var log = await _context.StudentLogs.FindAsync(id);
        if (log is null) return NotFound();
        if (!await HasAccessAsync(log.StudentId)) return Forbid();

        _context.StudentLogs.Remove(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }
}

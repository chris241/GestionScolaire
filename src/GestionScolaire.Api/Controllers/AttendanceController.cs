using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Attendances;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Attendance = GestionScolaire.Domain.Entities.Attendance;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public AttendanceController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<List<AttendanceDto>>> GetByClass([FromQuery] Guid classId, [FromQuery] DateTime date)
    {
        if (!await CanAccessClassAsync(classId)) return Forbid();

        var day = date.AsUtc().Date;

        var students = await _context.Students
            .Where(s => s.ClassId == classId && s.IsActive)
            .OrderBy(s => s.LastName)
            .ToListAsync();

        var attendances = await _context.Attendances
            .Where(a => a.ClassId == classId && a.Date == day)
            .ToListAsync();

        var result = students.Select(s =>
        {
            var attendance = attendances.FirstOrDefault(a => a.StudentId == s.Id);
            return new AttendanceDto(attendance?.Id, s.Id, s.FullName, classId, day, attendance?.Status.ToString(), attendance?.Comment);
        }).ToList();

        return Ok(result);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<AttendanceDto>>> GetByStudent(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        var query = _context.Attendances.Include(a => a.Student).Where(a => a.StudentId == studentId);

        // Un Parent (déjà vérifié ci-dessus via l'access policy) n'a pas de claim école. Pour tout autre
        // rôle le filtre reste actif : HasAccessAsync ne vérifie pas l'école pour un Directeur, c'est le
        // filtre Attendance (via Class) qui referme la frontière multi-tenant ici.
        if (_currentUser.Role == nameof(UserRole.Parent))
            query = query.IgnoreQueryFilters();

        var attendances = await query.OrderByDescending(a => a.Date).Take(60).ToListAsync();

        return Ok(attendances.Select(a => new AttendanceDto(a.Id, a.StudentId, a.Student.FullName, a.ClassId, a.Date, a.Status.ToString(), a.Comment)));
    }

    /// Outil de pointage en masse : marque la présence de toute une classe pour une date en une seule requête.
    [HttpPost("bulk")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<List<AttendanceDto>>> BulkMark(BulkMarkAttendanceRequest request)
    {
        if (!await CanAccessClassAsync(request.ClassId)) return Forbid();
        if (_currentUser.UserId is null) return Forbid();

        var day = request.Date.AsUtc().Date;

        var studentIds = request.Entries.Select(e => e.StudentId).ToList();
        var validStudentIds = await _context.Students
            .Where(s => s.ClassId == request.ClassId && studentIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        var existing = await _context.Attendances
            .Where(a => a.ClassId == request.ClassId && a.Date == day && studentIds.Contains(a.StudentId))
            .ToListAsync();

        foreach (var entry in request.Entries.Where(e => validStudentIds.Contains(e.StudentId)))
        {
            var attendance = existing.FirstOrDefault(a => a.StudentId == entry.StudentId);
            if (attendance is null)
            {
                attendance = new Attendance
                {
                    StudentId = entry.StudentId,
                    ClassId = request.ClassId,
                    Date = day,
                    RecordedByUserId = _currentUser.UserId.Value
                };
                _context.Attendances.Add(attendance);
            }

            attendance.Status = entry.Status;
            attendance.Comment = entry.Comment;
            attendance.RecordedByUserId = _currentUser.UserId.Value;
        }

        await _context.SaveChangesAsync();

        var students = await _context.Students
            .Where(s => s.ClassId == request.ClassId && s.IsActive)
            .OrderBy(s => s.LastName)
            .ToListAsync();

        var attendancesForDay = await _context.Attendances
            .Where(a => a.ClassId == request.ClassId && a.Date == day)
            .ToListAsync();

        var result = students.Select(s =>
        {
            var attendance = attendancesForDay.FirstOrDefault(a => a.StudentId == s.Id);
            return new AttendanceDto(attendance?.Id, s.Id, s.FullName, request.ClassId, day, attendance?.Status.ToString(), attendance?.Comment);
        }).ToList();

        return Ok(result);
    }

    /// Rapport des élèves absents ou en retard pour une date donnée (toute l'école pour le Directeur, sa classe pour l'enseignant).
    [HttpGet("reports/absent")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<List<AbsentStudentDto>>> GetAbsentReport([FromQuery] DateTime date, [FromQuery] Guid? classId)
    {
        if (classId.HasValue && !await CanAccessClassAsync(classId.Value)) return Forbid();

        var day = date.AsUtc().Date;

        var query = _context.Attendances
            .Include(a => a.Student)
            .Include(a => a.Class)
            .Where(a => a.Date == day && a.Status != AttendanceStatus.Present);

        if (classId.HasValue)
        {
            query = query.Where(a => a.ClassId == classId.Value);
        }
        else if (_currentUser.Role == nameof(UserRole.Teacher))
        {
            var teacherClassIds = _context.Classes
                .Where(c => c.HomeroomTeacher != null && c.HomeroomTeacher.UserId == _currentUser.UserId)
                .Select(c => c.Id);

            query = query.Where(a => teacherClassIds.Contains(a.ClassId));
        }

        var results = await query
            .OrderBy(a => a.Class.Name).ThenBy(a => a.Student.LastName)
            .Select(a => new AbsentStudentDto(a.StudentId, a.Student.FullName, a.ClassId, a.Class.Name, a.Status.ToString(), a.Comment))
            .ToListAsync();

        return Ok(results);
    }

    /// Feuille de présence mensuelle d'une classe : un jour du mois par colonne côté client.
    [HttpGet("reports/monthly")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<List<MonthlyAttendanceRowDto>>> GetMonthlySheet([FromQuery] Guid classId, [FromQuery] int year, [FromQuery] int month)
    {
        if (!await CanAccessClassAsync(classId)) return Forbid();
        if (month is < 1 or > 12) return BadRequest(new { message = "Mois invalide." });

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var students = await _context.Students
            .Where(s => s.ClassId == classId && s.IsActive)
            .OrderBy(s => s.LastName)
            .ToListAsync();

        var attendances = await _context.Attendances
            .Where(a => a.ClassId == classId && a.Date >= startDate && a.Date <= endDate)
            .ToListAsync();

        var result = students.Select(s => new MonthlyAttendanceRowDto(
            s.Id, s.FullName,
            attendances.Where(a => a.StudentId == s.Id).ToDictionary(a => a.Date.Day, a => a.Status.ToString()))
        ).ToList();

        return Ok(result);
    }

    /// Résumé de présence par lot d'élèves sur une période, tous lots confondus.
    [HttpGet("reports/batch-summary")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<BatchAttendanceSummaryDto>>> GetBatchSummary([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
    {
        var start = startDate.AsUtc().Date;
        var end = endDate.AsUtc().Date;
        if (end < start) return BadRequest(new { message = "La date de fin doit être postérieure à la date de début." });

        var students = await _context.Students
            .Include(s => s.StudentBatch)
            .Where(s => s.IsActive && s.StudentBatchId != null)
            .ToListAsync();

        var attendances = await _context.Attendances
            .Where(a => a.Date >= start && a.Date <= end)
            .ToListAsync();

        var batches = students
            .GroupBy(s => s.StudentBatch!)
            .Select(g => new BatchAttendanceSummaryDto(
                g.Key.Id, g.Key.Name,
                g.Select(s =>
                {
                    var studentAttendances = attendances.Where(a => a.StudentId == s.Id).ToList();
                    return new StudentAttendanceSummaryDto(
                        s.Id, s.FullName,
                        studentAttendances.Count(a => a.Status == AttendanceStatus.Present),
                        studentAttendances.Count(a => a.Status == AttendanceStatus.Absent),
                        studentAttendances.Count(a => a.Status == AttendanceStatus.Retard),
                        studentAttendances.Count(a => a.Status == AttendanceStatus.Excuse),
                        studentAttendances.Count);
                }).OrderBy(x => x.StudentFullName).ToList()))
            .OrderBy(b => b.BatchName)
            .ToList();

        return Ok(batches);
    }

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }

    private async Task<bool> CanAccessClassAsync(Guid classId)
    {
        if (_currentUser.Role != nameof(UserRole.Teacher)) return true;

        return await _context.Classes.AnyAsync(c =>
            c.Id == classId && c.HomeroomTeacher != null && c.HomeroomTeacher.UserId == _currentUser.UserId);
    }
}

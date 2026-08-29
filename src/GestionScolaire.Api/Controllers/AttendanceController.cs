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

        var attendances = await _context.Attendances
            .Include(a => a.Student)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.Date)
            .Take(60)
            .ToListAsync();

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

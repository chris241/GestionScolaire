using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.TeacherLogs;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeacherLogsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public TeacherLogsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet("teacher/{teacherId:guid}")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<List<TeacherLogDto>>> GetByTeacher(Guid teacherId)
    {
        if (!await HasAccessAsync(teacherId)) return Forbid();

        var logs = await _context.TeacherLogs
            .Where(l => l.TeacherId == teacherId)
            .OrderByDescending(l => l.LogDate)
            .Select(l => new TeacherLogDto(l.Id, l.TeacherId, l.LogDate, l.LogType, l.Description))
            .ToListAsync();

        return Ok(logs);
    }

    [HttpPost]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<TeacherLogDto>> Create(CreateTeacherLogRequest request)
    {
        if (!await HasAccessAsync(request.TeacherId)) return Forbid();

        var teacher = await _context.Teachers.FindAsync(request.TeacherId);
        if (teacher is null) return NotFound(new { message = "Enseignant introuvable." });

        var log = new TeacherLog
        {
            TeacherId = request.TeacherId,
            LogDate = request.LogDate.AsUtc(),
            LogType = request.LogType,
            Description = request.Description,
            RecordedByUserId = _currentUser.UserId!.Value
        };

        _context.TeacherLogs.Add(log);
        await _context.SaveChangesAsync();

        return Ok(new TeacherLogDto(log.Id, log.TeacherId, log.LogDate, log.LogType, log.Description));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<TeacherLogDto>> Update(Guid id, UpdateTeacherLogRequest request)
    {
        var log = await _context.TeacherLogs.FindAsync(id);
        if (log is null) return NotFound();
        if (!await HasAccessAsync(log.TeacherId)) return Forbid();

        log.LogDate = request.LogDate.AsUtc();
        log.LogType = request.LogType;
        log.Description = request.Description;
        await _context.SaveChangesAsync();

        return Ok(new TeacherLogDto(log.Id, log.TeacherId, log.LogDate, log.LogType, log.Description));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var log = await _context.TeacherLogs.FindAsync(id);
        if (log is null) return NotFound();
        if (!await HasAccessAsync(log.TeacherId)) return Forbid();

        _context.TeacherLogs.Remove(log);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> HasAccessAsync(Guid teacherId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        if (_currentUser.Role == "Director") return true;
        if (_currentUser.Role != "Teacher") return false;

        var teacher = await _context.Teachers.FindAsync(teacherId);
        return teacher is not null && teacher.UserId == _currentUser.UserId.Value;
    }
}

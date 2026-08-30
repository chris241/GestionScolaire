using GestionScolaire.Application.Common;
using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.LeaveApplications;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveApplicationsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public LeaveApplicationsController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet]
    [Authorize(Roles = "Director,Teacher,Parent")]
    public async Task<ActionResult<List<LeaveApplicationDto>>> GetAll([FromQuery] LeaveApplicationStatus? status)
    {
        var query = BaseQuery();

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (_currentUser.Role == nameof(UserRole.Parent))
        {
            // Un Parent n'a pas de claim école : son accès reste scopé élève par élève via StudentParent.
            query = query.IgnoreQueryFilters();

            var childIds = _context.StudentParents
                .Where(sp => sp.ParentUserId == _currentUser.UserId)
                .Select(sp => sp.StudentId);

            query = query.Where(l => childIds.Contains(l.StudentId));
        }
        else if (_currentUser.Role == nameof(UserRole.Teacher))
        {
            var teacherClassIds = _context.Classes
                .Where(c => c.HomeroomTeacher != null && c.HomeroomTeacher.UserId == _currentUser.UserId)
                .Select(c => c.Id);

            query = query.Where(l => teacherClassIds.Contains(l.Student.ClassId));
        }

        var applications = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

        return Ok(applications.Select(ToDto));
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<LeaveApplicationDto>>> GetByStudent(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        // Peut être appelé par un Parent (sans claim école), déjà vérifié ci-dessus via l'access policy.
        var applications = await BaseQuery().IgnoreQueryFilters()
            .Where(l => l.StudentId == studentId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        return Ok(applications.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Director,Parent")]
    public async Task<ActionResult<LeaveApplicationDto>> Create(CreateLeaveApplicationRequest request)
    {
        if (!await HasAccessAsync(request.StudentId)) return Forbid();
        if (_currentUser.UserId is null) return Forbid();

        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "La date de fin doit être postérieure à la date de début." });

        // Résolu via l'élève (pas via la claim école de l'appelant) : un Parent n'a pas de contexte
        // école, mais son enfant en a un via sa classe.
        var schoolId = await _context.Students.IgnoreQueryFilters()
            .Where(s => s.Id == request.StudentId)
            .Select(s => (Guid?)s.Class.SchoolId)
            .FirstOrDefaultAsync();
        if (schoolId is null) return NotFound(new { message = "Élève introuvable." });

        var application = new StudentLeaveApplication
        {
            SchoolId = schoolId.Value,
            StudentId = request.StudentId,
            StartDate = request.StartDate.AsUtc().Date,
            EndDate = request.EndDate.AsUtc().Date,
            Reason = request.Reason,
            RequestedByUserId = _currentUser.UserId.Value
        };

        _context.StudentLeaveApplications.Add(application);
        await _context.SaveChangesAsync();

        var full = await BaseQuery().IgnoreQueryFilters().FirstAsync(l => l.Id == application.Id);
        return Ok(ToDto(full));
    }

    [HttpPut("{id:guid}/decide")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<LeaveApplicationDto>> Decide(Guid id, DecideLeaveApplicationRequest request)
    {
        var application = await _context.StudentLeaveApplications.FindAsync(id);
        if (application is null) return NotFound();

        application.Status = request.Approve ? LeaveApplicationStatus.Approved : LeaveApplicationStatus.Rejected;
        application.DecisionDate = DateTime.UtcNow;
        application.DecisionNotes = request.DecisionNotes;

        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(l => l.Id == id);
        return Ok(ToDto(full));
    }

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }

    private IQueryable<StudentLeaveApplication> BaseQuery() => _context.StudentLeaveApplications.Include(l => l.Student);

    private static LeaveApplicationDto ToDto(StudentLeaveApplication l) => new(
        l.Id, l.StudentId, l.Student.FullName, l.StartDate, l.EndDate, l.Reason,
        l.Status.ToString(), l.DecisionDate, l.DecisionNotes);
}

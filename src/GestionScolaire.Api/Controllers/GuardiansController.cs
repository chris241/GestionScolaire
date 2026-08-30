using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Guardians;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GuardiansController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public GuardiansController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<GuardianDto>>> GetAll()
    {
        var guardians = await _context.Guardians
            .OrderBy(g => g.LastName)
            .Select(g => new GuardianDto(g.Id, g.FirstName, g.LastName, g.FullName, g.Phone, g.Email, g.Occupation, g.AreasOfInterest))
            .ToListAsync();

        return Ok(guardians);
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<GuardianDto>> Create(CreateGuardianRequest request)
    {
        var guardian = new Guardian
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Email = request.Email,
            Occupation = request.Occupation,
            AreasOfInterest = request.AreasOfInterest
        };

        _context.Guardians.Add(guardian);
        await _context.SaveChangesAsync();

        return Ok(new GuardianDto(guardian.Id, guardian.FirstName, guardian.LastName, guardian.FullName, guardian.Phone, guardian.Email, guardian.Occupation, guardian.AreasOfInterest));
    }

    [HttpPut("{id:guid}/interests")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<GuardianDto>> UpdateInterests(Guid id, UpdateGuardianInterestsRequest request)
    {
        var guardian = await _context.Guardians.FindAsync(id);
        if (guardian is null) return NotFound();

        guardian.AreasOfInterest = request.AreasOfInterest;
        await _context.SaveChangesAsync();

        return Ok(new GuardianDto(guardian.Id, guardian.FirstName, guardian.LastName, guardian.FullName, guardian.Phone, guardian.Email, guardian.Occupation, guardian.AreasOfInterest));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var guardian = await _context.Guardians.FindAsync(id);
        if (guardian is null) return NotFound();

        _context.Guardians.Remove(guardian);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<StudentGuardianDto>>> GetByStudent(Guid studentId)
    {
        if (!await HasAccessAsync(studentId)) return Forbid();

        var links = await _context.StudentGuardians
            .Include(sg => sg.Guardian)
            .Where(sg => sg.StudentId == studentId)
            .OrderByDescending(sg => sg.IsPrimaryContact)
            .Select(sg => new StudentGuardianDto(
                sg.Id, sg.GuardianId, sg.Guardian.FullName, sg.Guardian.Phone, sg.Guardian.Email, sg.Guardian.Occupation, sg.Guardian.AreasOfInterest,
                sg.Relationship, sg.IsPrimaryContact))
            .ToListAsync();

        return Ok(links);
    }

    [HttpPost("{guardianId:guid}/students/{studentId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<StudentGuardianDto>> LinkToStudent(Guid guardianId, Guid studentId, LinkGuardianRequest request)
    {
        var guardian = await _context.Guardians.FindAsync(guardianId);
        var student = await _context.Students.FindAsync(studentId);
        if (guardian is null || student is null) return NotFound(new { message = "Tuteur ou élève introuvable." });

        var alreadyLinked = await _context.StudentGuardians.AnyAsync(sg => sg.StudentId == studentId && sg.GuardianId == guardianId);
        if (alreadyLinked) return Conflict(new { message = "Ce tuteur est déjà rattaché à cet élève." });

        var link = new StudentGuardian
        {
            StudentId = studentId,
            GuardianId = guardianId,
            Relationship = request.Relationship,
            IsPrimaryContact = request.IsPrimaryContact
        };

        _context.StudentGuardians.Add(link);
        await _context.SaveChangesAsync();

        return Ok(new StudentGuardianDto(link.Id, guardian.Id, guardian.FullName, guardian.Phone, guardian.Email, guardian.Occupation, guardian.AreasOfInterest, link.Relationship, link.IsPrimaryContact));
    }

    [HttpDelete("{guardianId:guid}/students/{studentId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Unlink(Guid guardianId, Guid studentId)
    {
        var link = await _context.StudentGuardians.FirstOrDefaultAsync(sg => sg.StudentId == studentId && sg.GuardianId == guardianId);
        if (link is null) return NotFound();

        _context.StudentGuardians.Remove(link);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> HasAccessAsync(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return false;
        return await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId);
    }
}

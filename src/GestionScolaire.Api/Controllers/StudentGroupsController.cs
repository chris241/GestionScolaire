using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.StudentGroups;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentGroupsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public StudentGroupsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<StudentGroupDto>>> GetAll()
    {
        var groups = await _context.StudentGroups
            .Include(g => g.AcademicYear)
            .Include(g => g.Class)
            .Include(g => g.Members)
            .OrderBy(g => g.Name)
            .Select(g => new StudentGroupDto(
                g.Id, g.Name, g.GroupType, g.MaxSize,
                g.AcademicYearId, g.AcademicYear.Name,
                g.ClassId, g.Class == null ? null : g.Class.Name,
                g.Members.Count))
            .ToListAsync();

        return Ok(groups);
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<List<StudentGroupMemberDto>>> GetMembers(Guid id)
    {
        var members = await _context.StudentGroupMembers
            .Include(m => m.Student)
            .Where(m => m.StudentGroupId == id)
            .Select(m => new StudentGroupMemberDto(m.Id, m.StudentId, m.Student.FullName))
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<StudentGroupDto>> Create(CreateStudentGroupRequest request)
    {
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (year is null) return NotFound(new { message = "Année académique introuvable." });

        SchoolClass? schoolClass = null;
        if (request.ClassId.HasValue)
        {
            schoolClass = await _context.Classes.FindAsync(request.ClassId.Value);
            if (schoolClass is null) return NotFound(new { message = "Classe introuvable." });
        }

        var group = new StudentGroup
        {
            Name = request.Name,
            GroupType = request.GroupType,
            MaxSize = request.MaxSize,
            AcademicYearId = request.AcademicYearId,
            ClassId = request.ClassId
        };

        _context.StudentGroups.Add(group);
        await _context.SaveChangesAsync();

        return Ok(new StudentGroupDto(group.Id, group.Name, group.GroupType, group.MaxSize, group.AcademicYearId, year.Name, group.ClassId, schoolClass?.Name, 0));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var group = await _context.StudentGroups.FindAsync(id);
        if (group is null) return NotFound();

        _context.StudentGroups.Remove(group);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// Outil d'ajout en masse : rattache une liste d'élèves à un groupe en une seule requête.
    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<StudentGroupMemberDto>>> AddMembers(Guid id, AddGroupMembersRequest request)
    {
        var group = await _context.StudentGroups.FindAsync(id);
        if (group is null) return NotFound(new { message = "Groupe introuvable." });

        var existingStudentIds = await _context.StudentGroupMembers
            .Where(m => m.StudentGroupId == id)
            .Select(m => m.StudentId)
            .ToListAsync();

        var studentIdsToAdd = request.StudentIds.Distinct().Except(existingStudentIds).ToList();

        var validStudentIds = await _context.Students
            .Where(s => studentIdsToAdd.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var studentId in validStudentIds)
        {
            _context.StudentGroupMembers.Add(new StudentGroupMember { StudentGroupId = id, StudentId = studentId });
        }

        await _context.SaveChangesAsync();

        var members = await _context.StudentGroupMembers
            .Include(m => m.Student)
            .Where(m => m.StudentGroupId == id)
            .Select(m => new StudentGroupMemberDto(m.Id, m.StudentId, m.Student.FullName))
            .ToListAsync();

        return Ok(members);
    }

    [HttpDelete("{id:guid}/members/{studentId:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid studentId)
    {
        var member = await _context.StudentGroupMembers
            .FirstOrDefaultAsync(m => m.StudentGroupId == id && m.StudentId == studentId);

        if (member is null) return NotFound();

        _context.StudentGroupMembers.Remove(member);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

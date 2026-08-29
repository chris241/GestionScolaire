using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.AssessmentGroups;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssessmentGroupsController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public AssessmentGroupsController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<AssessmentGroupDto>>> GetAll([FromQuery] Guid? academicTermId)
    {
        var query = _context.AssessmentGroups.Include(g => g.AcademicTerm).AsQueryable();

        if (academicTermId.HasValue)
            query = query.Where(g => g.AcademicTermId == academicTermId.Value);

        var groups = await query.OrderBy(g => g.Name).ToListAsync();
        return Ok(groups.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<AssessmentGroupDto>> Create(CreateAssessmentGroupRequest request)
    {
        var term = await _context.AcademicTerms.FindAsync(request.AcademicTermId);
        if (term is null) return NotFound(new { message = "Trimestre introuvable." });

        var group = new AssessmentGroup
        {
            Name = request.Name,
            Weightage = request.Weightage,
            AcademicTermId = request.AcademicTermId
        };

        _context.AssessmentGroups.Add(group);
        await _context.SaveChangesAsync();

        return Ok(new AssessmentGroupDto(group.Id, group.Name, group.Weightage, group.AcademicTermId, term.Name));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var group = await _context.AssessmentGroups.FindAsync(id);
        if (group is null) return NotFound();

        _context.AssessmentGroups.Remove(group);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static AssessmentGroupDto ToDto(AssessmentGroup g) => new(g.Id, g.Name, g.Weightage, g.AcademicTermId, g.AcademicTerm.Name);
}

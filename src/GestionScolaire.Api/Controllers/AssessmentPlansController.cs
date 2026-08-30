using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.AssessmentPlans;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssessmentPlansController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AssessmentPlansController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<AssessmentPlanDto>>> GetAll([FromQuery] Guid? classId, [FromQuery] Guid? academicTermId)
    {
        var query = BaseQuery();

        if (classId.HasValue)
            query = query.Where(p => p.ClassId == classId.Value);

        if (academicTermId.HasValue)
            query = query.Where(p => p.AcademicTermId == academicTermId.Value);

        var plans = await query.OrderByDescending(p => p.PlannedDate).ToListAsync();
        return Ok(plans.Select(ToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<AssessmentPlanDto>> Create(CreateAssessmentPlanRequest request)
    {
        if (!await CanAccessClassAsync(request.ClassId)) return Forbid();

        var course = await _context.Courses.FindAsync(request.CourseId);
        var schoolClass = await _context.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId);
        var term = await _context.AcademicTerms.FindAsync(request.AcademicTermId);
        var group = await _context.AssessmentGroups.FindAsync(request.AssessmentGroupId);

        if (course is null || schoolClass is null || term is null || group is null)
            return NotFound(new { message = "Cours, classe, trimestre ou groupe d'évaluation introuvable." });

        if (request.GradingScaleId.HasValue && await _context.GradingScales.FindAsync(request.GradingScaleId.Value) is null)
            return NotFound(new { message = "Barème de notation introuvable." });

        var plan = new AssessmentPlan
        {
            Name = request.Name,
            MaxScore = request.MaxScore,
            PlannedDate = request.PlannedDate,
            CourseId = request.CourseId,
            ClassId = request.ClassId,
            AcademicTermId = request.AcademicTermId,
            AssessmentGroupId = request.AssessmentGroupId,
            GradingScaleId = request.GradingScaleId,
            SchoolId = _currentUser.SchoolId!.Value
        };

        _context.AssessmentPlans.Add(plan);
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(p => p.Id == plan.Id);
        return Ok(ToDto(full));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var plan = await _context.AssessmentPlans.FindAsync(id);
        if (plan is null) return NotFound();
        if (!await CanAccessClassAsync(plan.ClassId)) return Forbid();

        _context.AssessmentPlans.Remove(plan);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/criteria")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<AssessmentCriteriaDto>> AddCriteria(Guid id, CreateAssessmentCriteriaRequest request)
    {
        var plan = await _context.AssessmentPlans.FindAsync(id);
        if (plan is null) return NotFound();
        if (!await CanAccessClassAsync(plan.ClassId)) return Forbid();

        var criteria = new AssessmentCriteria { AssessmentPlanId = id, Name = request.Name, MaxScore = request.MaxScore };
        _context.AssessmentCriteria.Add(criteria);
        await _context.SaveChangesAsync();

        return Ok(new AssessmentCriteriaDto(criteria.Id, criteria.Name, criteria.MaxScore));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<ActionResult<AssessmentPlanDto>> UpdateStatus(Guid id, UpdateAssessmentPlanStatusRequest request)
    {
        var plan = await _context.AssessmentPlans.FindAsync(id);
        if (plan is null) return NotFound();
        if (!await CanAccessClassAsync(plan.ClassId)) return Forbid();

        plan.Status = request.Status;
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(p => p.Id == id);
        return Ok(ToDto(full));
    }

    [HttpDelete("criteria/{criteriaId:guid}")]
    [Authorize(Roles = "Director,Teacher")]
    public async Task<IActionResult> DeleteCriteria(Guid criteriaId)
    {
        var criteria = await _context.AssessmentCriteria.FindAsync(criteriaId);
        if (criteria is null) return NotFound();

        // AssessmentCriteria n'a pas son propre filtre (enfant pur de AssessmentPlan) : on vérifie
        // explicitement que le plan parent est bien accessible dans l'école active.
        if (await _context.AssessmentPlans.FindAsync(criteria.AssessmentPlanId) is null) return NotFound();

        _context.AssessmentCriteria.Remove(criteria);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> CanAccessClassAsync(Guid classId)
    {
        if (_currentUser.Role != nameof(UserRole.Teacher)) return true;

        return await _context.Classes.AnyAsync(c =>
            c.Id == classId && c.HomeroomTeacher != null && c.HomeroomTeacher.UserId == _currentUser.UserId);
    }

    private IQueryable<AssessmentPlan> BaseQuery() => _context.AssessmentPlans
        .Include(p => p.Course)
        .Include(p => p.Class)
        .Include(p => p.AcademicTerm)
        .Include(p => p.AssessmentGroup)
        .Include(p => p.Criteria);

    private static AssessmentPlanDto ToDto(AssessmentPlan p) => new(
        p.Id, p.Name, p.MaxScore, p.PlannedDate,
        p.CourseId, p.Course.Name,
        p.ClassId, p.Class.Name,
        p.AcademicTermId, p.AcademicTerm.Name,
        p.AssessmentGroupId, p.AssessmentGroup.Name,
        p.GradingScaleId, p.Status.ToString(),
        p.Criteria.Select(c => new AssessmentCriteriaDto(c.Id, c.Name, c.MaxScore)).ToList());
}

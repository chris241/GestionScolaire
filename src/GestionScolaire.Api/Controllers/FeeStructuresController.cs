using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.FeeStructures;
using GestionScolaire.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Director")]
public class FeeStructuresController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public FeeStructuresController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<FeeStructureDto>>> GetAll()
    {
        var structures = await BaseQuery().OrderBy(s => s.Name).ToListAsync();
        return Ok(structures.Select(ToDto));
    }

    [HttpPost]
    public async Task<ActionResult<FeeStructureDto>> Create(CreateFeeStructureRequest request)
    {
        var year = await _context.AcademicYears.FindAsync(request.AcademicYearId);
        if (year is null) return NotFound(new { message = "Année académique introuvable." });

        if (request.ProgramId.HasValue && await _context.AcademicPrograms.FindAsync(request.ProgramId.Value) is null)
            return NotFound(new { message = "Programme introuvable." });

        var structure = new FeeStructure { Name = request.Name, AcademicYearId = request.AcademicYearId, ProgramId = request.ProgramId };
        _context.FeeStructures.Add(structure);
        await _context.SaveChangesAsync();

        var full = await BaseQuery().FirstAsync(s => s.Id == structure.Id);
        return Ok(ToDto(full));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var structure = await _context.FeeStructures.FindAsync(id);
        if (structure is null) return NotFound();

        _context.FeeStructures.Remove(structure);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<FeeStructureItemDto>> AddItem(Guid id, CreateFeeStructureItemRequest request)
    {
        var structure = await _context.FeeStructures.FindAsync(id);
        var category = await _context.FeeCategories.FindAsync(request.FeeCategoryId);
        if (structure is null || category is null) return NotFound(new { message = "Structure ou catégorie de frais introuvable." });

        var item = new FeeStructureItem { FeeStructureId = id, FeeCategoryId = request.FeeCategoryId, Amount = request.Amount };
        _context.FeeStructureItems.Add(item);
        await _context.SaveChangesAsync();

        return Ok(new FeeStructureItemDto(item.Id, category.Id, category.Name, item.Amount));
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid itemId)
    {
        var item = await _context.FeeStructureItems.FindAsync(itemId);
        if (item is null) return NotFound();

        // FeeStructureItem n'a pas son propre filtre (enfant pur de FeeStructure) : on vérifie
        // explicitement que la structure parente est bien accessible dans l'école active.
        if (await _context.FeeStructures.FindAsync(item.FeeStructureId) is null) return NotFound();

        _context.FeeStructureItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:guid}/schedules")]
    public async Task<ActionResult<FeeScheduleDto>> AddSchedule(Guid id, CreateFeeScheduleRequest request)
    {
        var structure = await _context.FeeStructures.FindAsync(id);
        var term = await _context.AcademicTerms.FindAsync(request.AcademicTermId);
        if (structure is null || term is null) return NotFound(new { message = "Structure ou trimestre introuvable." });

        var schedule = new FeeSchedule { FeeStructureId = id, AcademicTermId = request.AcademicTermId, DueDate = request.DueDate };
        _context.FeeSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        return Ok(new FeeScheduleDto(schedule.Id, term.Id, term.Name, schedule.DueDate, 0));
    }

    [HttpDelete("schedules/{scheduleId:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid scheduleId)
    {
        var schedule = await _context.FeeSchedules.FindAsync(scheduleId);
        if (schedule is null) return NotFound();

        _context.FeeSchedules.Remove(schedule);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// Outil de génération en masse : crée une facture pour chaque élève concerné par une échéance, en une seule requête.
    /// Idempotent : une facture déjà générée pour un élève sur cette échéance n'est pas dupliquée.
    [HttpPost("schedules/{scheduleId:guid}/generate-invoices")]
    public async Task<ActionResult<GenerateInvoicesResult>> GenerateInvoices(Guid scheduleId)
    {
        var schedule = await _context.FeeSchedules
            .Include(s => s.FeeStructure).ThenInclude(f => f.Items)
            .Include(s => s.AcademicTerm)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule is null) return NotFound(new { message = "Échéance introuvable." });

        if (schedule.FeeStructure.Items.Sum(i => i.Amount) <= 0)
            return BadRequest(new { message = "La structure de frais ne comporte aucun élément facturable." });

        var (created, alreadyExisted) = await GenerateInvoicesForScheduleAsync(schedule);
        await _context.SaveChangesAsync();
        return Ok(new GenerateInvoicesResult(created, alreadyExisted));
    }

    /// Outil de génération en masse pour les frais mensuels : crée une échéance (et ses factures) pour
    /// chaque mois calendaire du trimestre choisi, en une seule requête. Idempotent au niveau du mois :
    /// un mois qui a déjà son échéance n'est pas dupliqué, la génération de factures l'est déjà en soi.
    [HttpPost("{id:guid}/schedules/monthly")]
    public async Task<ActionResult<GenerateMonthlySchedulesResult>> GenerateMonthlySchedules(Guid id, GenerateMonthlySchedulesRequest request)
    {
        var structure = await _context.FeeStructures
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id);
        var term = await _context.AcademicTerms.FindAsync(request.AcademicTermId);
        if (structure is null || term is null) return NotFound(new { message = "Structure ou trimestre introuvable." });

        if (structure.Items.Sum(i => i.Amount) <= 0)
            return BadRequest(new { message = "La structure de frais ne comporte aucun élément facturable." });

        var existingMonths = await _context.FeeSchedules
            .Where(s => s.FeeStructureId == id && s.AcademicTermId == request.AcademicTermId)
            .Select(s => new { s.DueDate.Year, s.DueDate.Month })
            .ToListAsync();

        var schedulesCreated = 0;
        var invoicesCreated = 0;
        var cursor = new DateTime(term.StartDate.Year, term.StartDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= term.EndDate)
        {
            if (!existingMonths.Any(m => m.Year == cursor.Year && m.Month == cursor.Month))
            {
                var dueDay = Math.Min(request.DueDayOfMonth, DateTime.DaysInMonth(cursor.Year, cursor.Month));
                var schedule = new FeeSchedule
                {
                    FeeStructureId = id,
                    AcademicTermId = request.AcademicTermId,
                    DueDate = new DateTime(cursor.Year, cursor.Month, dueDay, 0, 0, 0, DateTimeKind.Utc)
                };
                _context.FeeSchedules.Add(schedule);
                schedulesCreated++;

                schedule.FeeStructure = structure;
                var (created, _) = await GenerateInvoicesForScheduleAsync(schedule);
                invoicesCreated += created;
            }

            cursor = cursor.AddMonths(1);
        }

        await _context.SaveChangesAsync();

        return Ok(new GenerateMonthlySchedulesResult(schedulesCreated, existingMonths.Count, invoicesCreated));
    }

    private async Task<(int Created, int AlreadyExisted)> GenerateInvoicesForScheduleAsync(FeeSchedule schedule)
    {
        var totalAmount = schedule.FeeStructure.Items.Sum(i => i.Amount);

        var studentsQuery = _context.Students.Where(s => s.IsActive);

        if (schedule.FeeStructure.ProgramId.HasValue)
        {
            var enrolledStudentIds = _context.ProgramEnrollments
                .Where(e => e.ProgramId == schedule.FeeStructure.ProgramId.Value && e.AcademicYearId == schedule.FeeStructure.AcademicYearId)
                .Select(e => e.StudentId);

            studentsQuery = studentsQuery.Where(s => enrolledStudentIds.Contains(s.Id));
        }

        var students = await studentsQuery.ToListAsync();

        var existingStudentIds = await _context.Invoices
            .Where(i => i.FeeScheduleId == schedule.Id)
            .Select(i => i.StudentId)
            .ToListAsync();

        var toCreate = students.Where(s => !existingStudentIds.Contains(s.Id)).ToList();

        foreach (var student in toCreate)
        {
            _context.Invoices.Add(new Invoice
            {
                Student = student,
                FeeSchedule = schedule,
                SchoolId = _currentUser.SchoolId!.Value,
                InvoiceNumber = $"FAC-{schedule.DueDate:yyyyMM}-{student.EnrollmentNumber}",
                TotalAmount = totalAmount,
                DueDate = schedule.DueDate
            });
        }

        return (toCreate.Count, existingStudentIds.Count);
    }

    private IQueryable<FeeStructure> BaseQuery() => _context.FeeStructures
        .Include(s => s.AcademicYear)
        .Include(s => s.Program)
        .Include(s => s.Items).ThenInclude(i => i.FeeCategory)
        .Include(s => s.Schedules).ThenInclude(sc => sc.AcademicTerm)
        .Include(s => s.Schedules).ThenInclude(sc => sc.Invoices);

    private static FeeStructureDto ToDto(FeeStructure s) => new(
        s.Id, s.Name, s.AcademicYearId, s.AcademicYear.Name, s.ProgramId, s.Program?.Name,
        s.Items.Sum(i => i.Amount),
        s.Items.Select(i => new FeeStructureItemDto(i.Id, i.FeeCategoryId, i.FeeCategory.Name, i.Amount)).ToList(),
        s.Schedules.Select(sc => new FeeScheduleDto(sc.Id, sc.AcademicTermId, sc.AcademicTerm.Name, sc.DueDate, sc.Invoices.Count)).ToList());
}

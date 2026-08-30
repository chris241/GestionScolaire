using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public InvoicesController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<InvoiceDto>>> GetAll([FromQuery] int take = 50)
    {
        var invoices = await BaseQuery().OrderByDescending(i => i.GeneratedAt).Take(take).ToListAsync();
        return Ok(invoices.Select(ToDto));
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<InvoiceDto>>> GetByStudent(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return Forbid();
        if (!await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId))
            return Forbid();

        var invoices = await BaseQuery()
            .Where(i => i.StudentId == studentId)
            .OrderByDescending(i => i.DueDate)
            .ToListAsync();

        return Ok(invoices.Select(ToDto));
    }

    /// Collecte des frais par élève : montant facturé, encaissé et restant dû (factures impayées uniquement).
    [HttpGet("reports/student-collection")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<StudentFeeCollectionDto>>> GetStudentCollectionReport([FromQuery] Guid? classId)
    {
        var studentsQuery = _context.Students.IgnoreQueryFilters().Include(s => s.Class).Where(s => s.IsActive);
        if (classId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.ClassId == classId.Value);

        var students = await studentsQuery.ToListAsync();
        var studentIds = students.Select(s => s.Id).ToHashSet();

        var invoices = await _context.Invoices.Where(i => studentIds.Contains(i.StudentId)).ToListAsync();
        var payments = await _context.Payments
            .Where(p => studentIds.Contains(p.StudentId) && p.Status == Domain.Enums.PaymentStatus.Paye)
            .ToListAsync();

        var result = students
            .Select(s => new StudentFeeCollectionDto(
                s.Id, s.FullName, s.Class.Name,
                invoices.Where(i => i.StudentId == s.Id).Sum(i => i.TotalAmount),
                payments.Where(p => p.StudentId == s.Id).Sum(p => p.Amount),
                invoices.Where(i => i.StudentId == s.Id && i.Status != Domain.Enums.PaymentStatus.Paye).Sum(i => i.TotalAmount)))
            .OrderByDescending(r => r.OutstandingAmount)
            .ToList();

        return Ok(result);
    }

    /// Collecte des frais par programme : agrège la facturation de tous les élèves inscrits à chaque programme.
    [HttpGet("reports/program-collection")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<ProgramFeeCollectionDto>>> GetProgramCollectionReport()
    {
        var enrollments = await _context.ProgramEnrollments
            .Include(e => e.Program)
            .Where(e => e.Status == Domain.Enums.EnrollmentStatus.Active)
            .ToListAsync();

        var studentIds = enrollments.Select(e => e.StudentId).ToHashSet();
        var invoices = await _context.Invoices.Where(i => studentIds.Contains(i.StudentId)).ToListAsync();
        var payments = await _context.Payments
            .Where(p => studentIds.Contains(p.StudentId) && p.Status == Domain.Enums.PaymentStatus.Paye)
            .ToListAsync();

        var result = enrollments
            .GroupBy(e => e.Program)
            .Select(g =>
            {
                var programStudentIds = g.Select(e => e.StudentId).ToHashSet();
                return new ProgramFeeCollectionDto(
                    g.Key.Id, g.Key.Name, programStudentIds.Count,
                    invoices.Where(i => programStudentIds.Contains(i.StudentId)).Sum(i => i.TotalAmount),
                    payments.Where(p => programStudentIds.Contains(p.StudentId)).Sum(p => p.Amount),
                    invoices.Where(i => programStudentIds.Contains(i.StudentId) && i.Status != Domain.Enums.PaymentStatus.Paye).Sum(i => i.TotalAmount));
            })
            .OrderBy(r => r.ProgramName)
            .ToList();

        return Ok(result);
    }

    private IQueryable<Domain.Entities.Invoice> BaseQuery() => _context.Invoices.IgnoreQueryFilters()
        .Include(i => i.Student)
        .Include(i => i.FeeSchedule).ThenInclude(s => s.AcademicTerm)
        .Include(i => i.FeeSchedule).ThenInclude(s => s.FeeStructure);

    private static InvoiceDto ToDto(Domain.Entities.Invoice i) => new(
        i.Id, i.StudentId, i.Student.FullName, i.InvoiceNumber, i.TotalAmount, i.DueDate, i.Status.ToString(),
        i.FeeScheduleId, i.FeeSchedule.FeeStructure.Name, i.FeeSchedule.AcademicTerm.Name);
}

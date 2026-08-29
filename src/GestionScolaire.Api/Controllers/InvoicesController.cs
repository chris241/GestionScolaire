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

    private IQueryable<Domain.Entities.Invoice> BaseQuery() => _context.Invoices
        .Include(i => i.Student)
        .Include(i => i.FeeSchedule).ThenInclude(s => s.AcademicTerm)
        .Include(i => i.FeeSchedule).ThenInclude(s => s.FeeStructure);

    private static InvoiceDto ToDto(Domain.Entities.Invoice i) => new(
        i.Id, i.StudentId, i.Student.FullName, i.InvoiceNumber, i.TotalAmount, i.DueDate, i.Status.ToString(),
        i.FeeScheduleId, i.FeeSchedule.FeeStructure.Name, i.FeeSchedule.AcademicTerm.Name);
}

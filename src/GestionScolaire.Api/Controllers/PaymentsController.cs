using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStudentAccessPolicy _accessPolicy;

    public PaymentsController(IApplicationDbContext context, ICurrentUserService currentUser, IStudentAccessPolicy accessPolicy)
    {
        _context = context;
        _currentUser = currentUser;
        _accessPolicy = accessPolicy;
    }

    [HttpGet]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<PaymentDto>>> GetAll([FromQuery] int take = 20)
    {
        var payments = await _context.Payments
            .Include(p => p.Student)
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .Select(p => new PaymentDto(
                p.Id, p.StudentId, p.Student.FullName, p.Description,
                p.Amount, p.DueDate, p.PaidAt, p.Status.ToString()))
            .ToListAsync();

        return Ok(payments);
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<PaymentDto>>> GetByStudent(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return Forbid();
        if (!await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId))
            return Forbid();

        var payments = await _context.Payments
            .Include(p => p.Student)
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.DueDate)
            .Select(p => new PaymentDto(
                p.Id, p.StudentId, p.Student.FullName, p.Description,
                p.Amount, p.DueDate, p.PaidAt, p.Status.ToString()))
            .ToListAsync();

        return Ok(payments);
    }
}

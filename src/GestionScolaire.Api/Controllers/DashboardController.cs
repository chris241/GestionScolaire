using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Dashboard;
using GestionScolaire.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Director")]
public class DashboardController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public DashboardController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var enrolledStudents = await _context.Students.CountAsync(s => s.IsActive);
        var teachers = await _context.Teachers.CountAsync();

        var totalDue = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0;
        var totalPaid = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Paye)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var recoveryRate = totalDue == 0 ? 0 : Math.Round(totalPaid / totalDue * 100, 1);

        var today = DateTime.UtcNow.Date;
        var todayAbsences = await _context.Attendances
            .CountAsync(a => a.Date == today && a.Status == AttendanceStatus.Absent);

        return Ok(new DashboardStatsDto(enrolledStudents, teachers, recoveryRate, todayAbsences));
    }

    [HttpGet("recent-activity")]
    public async Task<ActionResult<List<RecentActivityDto>>> GetRecentActivity([FromQuery] int take = 10)
    {
        var payments = await _context.Payments
            .Include(p => p.Student)
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .Select(p => new RecentActivityDto(
                p.Id,
                p.Student.FullName,
                "Paiement",
                p.Description,
                p.Amount,
                p.Status.ToString(),
                p.CreatedAt))
            .ToListAsync();

        return Ok(payments);
    }
}

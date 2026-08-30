using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Payments;
using GestionScolaire.Domain.Entities;
using GestionScolaire.Domain.Enums;
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
                p.Amount, p.DueDate, p.PaidAt, p.Status.ToString(), p.InvoiceId))
            .ToListAsync();

        return Ok(payments);
    }

    /// Le Directeur enregistre un paiement déjà reçu (espèces, Mobile Money...). Aucune intégration
    /// de paiement en ligne n'est effectuée ni prévue.
    [HttpPost]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<PaymentDto>> Create(CreatePaymentRequest request)
    {
        var student = await _context.Students.FindAsync(request.StudentId);
        if (student is null) return NotFound(new { message = "Élève introuvable." });

        Invoice? invoice = null;
        if (request.InvoiceId.HasValue)
        {
            invoice = await _context.Invoices.FindAsync(request.InvoiceId.Value);
            if (invoice is null) return NotFound(new { message = "Facture introuvable." });
            if (invoice.StudentId != request.StudentId)
                return BadRequest(new { message = "Cette facture ne correspond pas à l'élève sélectionné." });
        }

        var now = DateTime.UtcNow;
        var payment = new Payment
        {
            StudentId = request.StudentId,
            Description = request.Description,
            Amount = request.Amount,
            AcademicYear = request.AcademicYear,
            Term = request.Term,
            DueDate = now,
            PaidAt = now,
            Status = PaymentStatus.Paye,
            Method = request.Method,
            InvoiceId = request.InvoiceId
        };

        _context.Payments.Add(payment);

        if (invoice is not null)
            invoice.Status = PaymentStatus.Paye;

        await _context.SaveChangesAsync();

        return Ok(new PaymentDto(
            payment.Id, payment.StudentId, student.FullName, payment.Description,
            payment.Amount, payment.DueDate, payment.PaidAt, payment.Status.ToString(), payment.InvoiceId));
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<PaymentDto>>> GetByStudent(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return Forbid();
        if (!await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId))
            return Forbid();

        var payments = await _context.Payments.IgnoreQueryFilters()
            .Include(p => p.Student)
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.DueDate)
            .Select(p => new PaymentDto(
                p.Id, p.StudentId, p.Student.FullName, p.Description,
                p.Amount, p.DueDate, p.PaidAt, p.Status.ToString(), p.InvoiceId))
            .ToListAsync();

        return Ok(payments);
    }
}

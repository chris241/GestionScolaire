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
    public async Task<ActionResult<List<PaymentDto>>> GetAll([FromQuery] int take = 20, [FromQuery] string? academicYear = null)
    {
        var query = _context.Payments.Include(p => p.Student).AsQueryable();

        if (!string.IsNullOrWhiteSpace(academicYear))
            query = query.Where(p => p.AcademicYear == academicYear);

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync();

        return Ok(payments.Select(p => ToDto(p, p.Student.FullName)));
    }

    /// File d'attente des paiements déclarés par les Parents, en attente de validation par le Directeur.
    [HttpGet("pending")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<List<PaymentDto>>> GetPending()
    {
        var payments = await _context.Payments
            .Include(p => p.Student)
            .Where(p => p.Status == PaymentStatus.EnValidation)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();

        return Ok(payments.Select(p => ToDto(p, p.Student.FullName)));
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
            SchoolId = _currentUser.SchoolId!.Value,
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

        return Ok(ToDto(payment, student.FullName));
    }

    /// Un Parent déclare avoir réglé un montant hors app pour l'un de ses enfants ; le paiement reste
    /// "en attente de validation" (Status = EnValidation) jusqu'à confirmation du Directeur — voir
    /// Validate/Reject ci-dessous. Aucune intégration de paiement en ligne : l'argent ne transite jamais
    /// par l'app, seule la déclaration + son contrôle par le Directeur le font.
    [HttpPost("declare")]
    [Authorize(Roles = "Parent")]
    public async Task<ActionResult<PaymentDto>> Declare(DeclarePaymentRequest request)
    {
        if (_currentUser.UserId is null) return Forbid();
        if (!await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, nameof(UserRole.Parent), request.StudentId))
            return Forbid();

        // Un Parent n'a pas de claim école (voir AuthController) : l'élève, lui, en a une via sa classe.
        var student = await _context.Students.IgnoreQueryFilters()
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.Id == request.StudentId);
        if (student is null) return NotFound(new { message = "Élève introuvable." });

        Invoice? invoice = null;
        if (request.InvoiceId.HasValue)
        {
            invoice = await _context.Invoices.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId.Value);
            if (invoice is null) return NotFound(new { message = "Facture introuvable." });
            if (invoice.StudentId != request.StudentId)
                return BadRequest(new { message = "Cette facture ne correspond pas à l'élève sélectionné." });
        }

        var payment = new Payment
        {
            StudentId = request.StudentId,
            SchoolId = student.Class.SchoolId,
            Description = request.Description,
            Amount = request.Amount,
            AcademicYear = request.AcademicYear,
            Term = request.Term,
            DueDate = invoice?.DueDate ?? DateTime.UtcNow,
            Status = PaymentStatus.EnValidation,
            Method = request.Method,
            InvoiceId = request.InvoiceId,
            DeclaredByUserId = _currentUser.UserId.Value
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return Ok(ToDto(payment, student.FullName));
    }

    /// Le Directeur confirme qu'une déclaration de paiement d'un Parent a bien été reçue.
    [HttpPut("{id:guid}/validate")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<PaymentDto>> Validate(Guid id)
    {
        var payment = await _context.Payments.Include(p => p.Student).FirstOrDefaultAsync(p => p.Id == id);
        if (payment is null) return NotFound();
        if (payment.Status != PaymentStatus.EnValidation)
            return BadRequest(new { message = "Ce paiement n'est pas en attente de validation." });

        var now = DateTime.UtcNow;
        payment.Status = PaymentStatus.Paye;
        payment.PaidAt = now;
        payment.DecisionDate = now;

        if (payment.InvoiceId.HasValue)
        {
            var invoice = await _context.Invoices.FindAsync(payment.InvoiceId.Value);
            if (invoice is not null) invoice.Status = PaymentStatus.Paye;
        }

        await _context.SaveChangesAsync();

        return Ok(ToDto(payment, payment.Student.FullName));
    }

    /// Le Directeur rejette une déclaration de paiement (ex: référence introuvable, montant erroné).
    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Director")]
    public async Task<ActionResult<PaymentDto>> Reject(Guid id, RejectPaymentRequest request)
    {
        var payment = await _context.Payments.Include(p => p.Student).FirstOrDefaultAsync(p => p.Id == id);
        if (payment is null) return NotFound();
        if (payment.Status != PaymentStatus.EnValidation)
            return BadRequest(new { message = "Ce paiement n'est pas en attente de validation." });

        payment.Status = PaymentStatus.Annule;
        payment.DecisionDate = DateTime.UtcNow;
        payment.DecisionNotes = request.DecisionNotes;

        await _context.SaveChangesAsync();

        return Ok(ToDto(payment, payment.Student.FullName));
    }

    [HttpGet("student/{studentId:guid}")]
    public async Task<ActionResult<List<PaymentDto>>> GetByStudent(Guid studentId)
    {
        if (_currentUser.UserId is null || _currentUser.Role is null) return Forbid();
        if (!await _accessPolicy.CanAccessStudentAsync(_currentUser.UserId.Value, _currentUser.Role, studentId))
            return Forbid();

        var query = _context.Payments.Include(p => p.Student).Where(p => p.StudentId == studentId);

        // Un Parent (déjà vérifié ci-dessus via l'access policy) n'a pas de claim école. Pour tout autre
        // rôle le filtre reste actif : CanAccessStudentAsync ne vérifie pas l'école pour un Directeur,
        // c'est le filtre Student (via Class) qui referme la frontière multi-tenant ici.
        if (_currentUser.Role == nameof(UserRole.Parent))
            query = query.IgnoreQueryFilters();

        var payments = await query.OrderByDescending(p => p.DueDate).ToListAsync();

        return Ok(payments.Select(p => ToDto(p, p.Student.FullName)));
    }

    private static PaymentDto ToDto(Payment p, string studentFullName) => new(
        p.Id, p.StudentId, studentFullName, p.Description, p.Amount, p.DueDate, p.PaidAt,
        p.Status.ToString(), p.Method, p.DecisionNotes, p.InvoiceId);
}

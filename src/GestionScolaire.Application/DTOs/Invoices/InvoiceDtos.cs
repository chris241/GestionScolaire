namespace GestionScolaire.Application.DTOs.Invoices;

public record InvoiceDto(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    string InvoiceNumber,
    decimal TotalAmount,
    DateTime DueDate,
    string Status,
    Guid FeeScheduleId,
    string FeeStructureName,
    string AcademicTermName
);

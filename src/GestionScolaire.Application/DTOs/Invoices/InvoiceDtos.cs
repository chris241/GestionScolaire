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

public record StudentFeeCollectionDto(
    Guid StudentId,
    string StudentFullName,
    string ClassName,
    decimal InvoicedAmount,
    decimal PaidAmount,
    decimal OutstandingAmount
);

public record ProgramFeeCollectionDto(
    Guid ProgramId,
    string ProgramName,
    int StudentCount,
    decimal InvoicedAmount,
    decimal PaidAmount,
    decimal OutstandingAmount
);

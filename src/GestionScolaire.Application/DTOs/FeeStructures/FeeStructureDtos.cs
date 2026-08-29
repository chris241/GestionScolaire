using System.ComponentModel.DataAnnotations;

namespace GestionScolaire.Application.DTOs.FeeStructures;

public record FeeStructureItemDto(Guid Id, Guid FeeCategoryId, string FeeCategoryName, decimal Amount);

public record FeeScheduleDto(Guid Id, Guid AcademicTermId, string AcademicTermName, DateTime DueDate, int InvoiceCount);

public record FeeStructureDto(
    Guid Id,
    string Name,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid? ProgramId,
    string? ProgramName,
    decimal TotalAmount,
    List<FeeStructureItemDto> Items,
    List<FeeScheduleDto> Schedules
);

public record CreateFeeStructureRequest(
    [Required] string Name,
    [Required] Guid AcademicYearId,
    Guid? ProgramId
);

public record CreateFeeStructureItemRequest([Required] Guid FeeCategoryId, [Required] decimal Amount);

public record CreateFeeScheduleRequest([Required] Guid AcademicTermId, [Required] DateTime DueDate);

public record GenerateInvoicesResult(int Created, int AlreadyExisted);

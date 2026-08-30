namespace GestionScolaire.Application.DTOs.Students;

public record StudentImportRowResult(
    int RowNumber,
    bool Success,
    string FirstName,
    string LastName,
    Guid? StudentId,
    string? ErrorMessage
);

public record StudentImportResultDto(
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    List<StudentImportRowResult> Rows
);

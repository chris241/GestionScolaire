namespace GestionScolaire.Application.DTOs.Dashboard;

public record DashboardStatsDto(
    int EnrolledStudents,
    int Teachers,
    decimal RecoveryRate,
    int TodayAbsences
);

public record RecentActivityDto(
    Guid Id,
    string StudentFullName,
    string Type,
    string Description,
    decimal? Amount,
    string Status,
    DateTime Date
);

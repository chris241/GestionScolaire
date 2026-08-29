using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Teacher> Teachers { get; }
    DbSet<Student> Students { get; }
    DbSet<StudentParent> StudentParents { get; }
    DbSet<SchoolClass> Classes { get; }
    DbSet<Subject> Subjects { get; }
    DbSet<Grade> Grades { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<Payment> Payments { get; }
    DbSet<AcademicYear> AcademicYears { get; }
    DbSet<AcademicTerm> AcademicTerms { get; }
    DbSet<EducationSettings> EducationSettings { get; }
    DbSet<StudentCategory> StudentCategories { get; }
    DbSet<StudentBatch> StudentBatches { get; }
    DbSet<StudentGroup> StudentGroups { get; }
    DbSet<StudentGroupMember> StudentGroupMembers { get; }
    DbSet<StudentLog> StudentLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

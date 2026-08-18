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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

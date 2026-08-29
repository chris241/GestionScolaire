using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentParent> StudentParents => Set<StudentParent>();
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
    public DbSet<EducationSettings> EducationSettings => Set<EducationSettings>();
    public DbSet<StudentCategory> StudentCategories => Set<StudentCategory>();
    public DbSet<StudentBatch> StudentBatches => Set<StudentBatch>();
    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();
    public DbSet<StudentGroupMember> StudentGroupMembers => Set<StudentGroupMember>();
    public DbSet<StudentLog> StudentLogs => Set<StudentLog>();
    public DbSet<StudentApplicant> StudentApplicants => Set<StudentApplicant>();
    public DbSet<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<CourseSchedule> CourseSchedules => Set<CourseSchedule>();
    public DbSet<ProgramEnrollment> ProgramEnrollments => Set<ProgramEnrollment>();
    public DbSet<StudentLeaveApplication> StudentLeaveApplications => Set<StudentLeaveApplication>();
    public DbSet<GradingScale> GradingScales => Set<GradingScale>();
    public DbSet<GradingScaleInterval> GradingScaleIntervals => Set<GradingScaleInterval>();
    public DbSet<AssessmentGroup> AssessmentGroups => Set<AssessmentGroup>();
    public DbSet<AssessmentPlan> AssessmentPlans => Set<AssessmentPlan>();
    public DbSet<AssessmentCriteria> AssessmentCriteria => Set<AssessmentCriteria>();
    public DbSet<FeeCategory> FeeCategories => Set<FeeCategory>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeeStructureItem> FeeStructureItems => Set<FeeStructureItem>();
    public DbSet<FeeSchedule> FeeSchedules => Set<FeeSchedule>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();
    public DbSet<StudentSibling> StudentSiblings => Set<StudentSibling>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

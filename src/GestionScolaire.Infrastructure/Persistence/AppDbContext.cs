using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionScolaire.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser) : base(options)
    {
        _currentUser = currentUser;
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
    public DbSet<School> Schools => Set<School>();
    public DbSet<TeacherSchool> TeacherSchools => Set<TeacherSchool>();
    public DbSet<StudentCategory> StudentCategories => Set<StudentCategory>();
    public DbSet<StudentBatch> StudentBatches => Set<StudentBatch>();
    public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();
    public DbSet<StudentGroupMember> StudentGroupMembers => Set<StudentGroupMember>();
    public DbSet<StudentLog> StudentLogs => Set<StudentLog>();
    public DbSet<TeacherLog> TeacherLogs => Set<TeacherLog>();
    public DbSet<StudentApplicant> StudentApplicants => Set<StudentApplicant>();
    public DbSet<AdmissionCampaign> AdmissionCampaigns => Set<AdmissionCampaign>();
    public DbSet<AdmissionCampaignQuota> AdmissionCampaignQuotas => Set<AdmissionCampaignQuota>();
    public DbSet<AcademicProgram> AcademicPrograms => Set<AcademicProgram>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<CourseSchedule> CourseSchedules => Set<CourseSchedule>();
    public DbSet<ProgramEnrollment> ProgramEnrollments => Set<ProgramEnrollment>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
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

        modelBuilder.Entity<SchoolClass>().HasQueryFilter(c => c.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Teacher>().HasQueryFilter(t => t.Schools.Any(ts => ts.SchoolId == _currentUser.SchoolId));
        modelBuilder.Entity<AcademicYear>().HasQueryFilter(y => y.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<AcademicTerm>().HasQueryFilter(t => t.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<AcademicProgram>().HasQueryFilter(p => p.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Room>().HasQueryFilter(r => r.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<StudentCategory>().HasQueryFilter(c => c.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<StudentBatch>().HasQueryFilter(b => b.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<StudentGroup>().HasQueryFilter(g => g.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Student>().HasQueryFilter(s => s.Class.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<StudentApplicant>().HasQueryFilter(a => a.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<AdmissionCampaign>().HasQueryFilter(c => c.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Subject>().HasQueryFilter(s => s.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Course>().HasQueryFilter(c => c.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<CourseSchedule>().HasQueryFilter(s => s.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<ProgramEnrollment>().HasQueryFilter(e => e.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<CourseEnrollment>().HasQueryFilter(e => e.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Attendance>().HasQueryFilter(a => a.Class.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<StudentLeaveApplication>().HasQueryFilter(l => l.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<GradingScale>().HasQueryFilter(s => s.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<AssessmentGroup>().HasQueryFilter(g => g.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<AssessmentPlan>().HasQueryFilter(p => p.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Grade>().HasQueryFilter(g => g.Class.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<FeeCategory>().HasQueryFilter(c => c.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<FeeStructure>().HasQueryFilter(s => s.AcademicYear.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<FeeSchedule>().HasQueryFilter(s => s.AcademicTerm.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Invoice>().HasQueryFilter(i => i.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Payment>().HasQueryFilter(p => p.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<Guardian>().HasQueryFilter(g => g.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<StudentGuardian>().HasQueryFilter(sg => sg.Guardian.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<StudentLog>().HasQueryFilter(l => l.SchoolId == _currentUser.SchoolId);
        modelBuilder.Entity<TeacherLog>().HasQueryFilter(l => l.SchoolId == _currentUser.SchoolId);

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

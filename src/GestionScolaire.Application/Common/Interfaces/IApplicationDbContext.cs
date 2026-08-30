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
    DbSet<TeacherLog> TeacherLogs { get; }
    DbSet<StudentApplicant> StudentApplicants { get; }
    DbSet<AcademicProgram> AcademicPrograms { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Course> Courses { get; }
    DbSet<Topic> Topics { get; }
    DbSet<CourseSchedule> CourseSchedules { get; }
    DbSet<ProgramEnrollment> ProgramEnrollments { get; }
    DbSet<StudentLeaveApplication> StudentLeaveApplications { get; }
    DbSet<GradingScale> GradingScales { get; }
    DbSet<GradingScaleInterval> GradingScaleIntervals { get; }
    DbSet<AssessmentGroup> AssessmentGroups { get; }
    DbSet<AssessmentPlan> AssessmentPlans { get; }
    DbSet<AssessmentCriteria> AssessmentCriteria { get; }
    DbSet<FeeCategory> FeeCategories { get; }
    DbSet<FeeStructure> FeeStructures { get; }
    DbSet<FeeStructureItem> FeeStructureItems { get; }
    DbSet<FeeSchedule> FeeSchedules { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<Guardian> Guardians { get; }
    DbSet<StudentGuardian> StudentGuardians { get; }
    DbSet<StudentSibling> StudentSiblings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

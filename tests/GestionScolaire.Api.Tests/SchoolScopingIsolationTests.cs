using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Admissions;
using GestionScolaire.Application.DTOs.AcademicTerms;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.AssessmentGroups;
using GestionScolaire.Application.DTOs.AssessmentPlans;
using GestionScolaire.Application.DTOs.Attendances;
using GestionScolaire.Application.DTOs.Auth;
using GestionScolaire.Application.DTOs.CourseEnrollments;
using GestionScolaire.Application.DTOs.CourseSchedules;
using GestionScolaire.Application.DTOs.Courses;
using GestionScolaire.Application.DTOs.FeeCategories;
using GestionScolaire.Application.DTOs.FeeStructures;
using GestionScolaire.Application.DTOs.Grades;
using GestionScolaire.Application.DTOs.GradingScales;
using GestionScolaire.Application.DTOs.Guardians;
using GestionScolaire.Application.DTOs.Invoices;
using GestionScolaire.Application.DTOs.LeaveApplications;
using GestionScolaire.Application.DTOs.Payments;
using GestionScolaire.Application.DTOs.StudentLogs;
using GestionScolaire.Application.DTOs.ProgramEnrollments;
using GestionScolaire.Application.DTOs.Programs;
using GestionScolaire.Application.DTOs.Rooms;
using GestionScolaire.Application.DTOs.Schools;
using GestionScolaire.Application.DTOs.Students;
using GestionScolaire.Application.DTOs.StudentBatches;
using GestionScolaire.Application.DTOs.StudentCategories;
using GestionScolaire.Application.DTOs.StudentGroups;
using GestionScolaire.Application.DTOs.Subjects;
using Xunit;

namespace GestionScolaire.Api.Tests;

/// Phase 1 : AcademicYear, AcademicTerm, AcademicProgram, Room, StudentCategory, StudentBatch, StudentGroup
/// et Student (via Class) sont désormais scopés par école. Phase 2 : StudentApplicant et AdmissionCampaign
/// (avec le formulaire public /candidature qui précise désormais l'école visée). Phase 3 : Subject, Course,
/// CourseSchedule, ProgramEnrollment, CourseEnrollment. Phase 4 : Attendance (via Class, sans colonne
/// propre) et StudentLeaveApplication. Phase 5 : GradingScale, AssessmentGroup, AssessmentPlan (colonne
/// propre) et Grade (via Class, sans colonne propre). Phase 6 : FeeCategory, Invoice, Payment (colonne
/// propre) ; FeeStructure (via AcademicYear) et FeeSchedule (via AcademicTerm), sans colonne propre.
/// Phase 7 : Guardian, StudentLog, TeacherLog (colonne propre) ; StudentGuardian (via Guardian, sans
/// colonne propre) et StudentSibling (aucune colonne, protégé transitivement via le filtre de Student
/// sur ses deux navigations). Ces tests vérifient qu'un directeur tout juste inscrit, propriétaire d'une
/// école fraîchement créée, ne voit jamais les données déjà seedées pour Lumière/Génie — c'est la
/// garantie de sécurité la plus importante de ces phases.
[Collection(ApiTestCollection.Name)]
public class SchoolScopingIsolationTests
{
    private readonly ApiWebApplicationFactory _factory;

    public SchoolScopingIsolationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> RegisterDirectorWithFreshSchoolAsync()
    {
        var email = $"isole.phase1.{Guid.NewGuid():N}@ecole.mg";
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, AuthHelper.DemoPassword, "Isole", "Directeur", "Director"));
        var authedClient = await client.AsUserAsync(email);

        await authedClient.PostAsJsonAsync("/api/schools", new GestionScolaire.Application.DTOs.Schools.CreateSchoolRequest(
            "École Neuve", null, "MGA", 20));

        // Re-login pour que le token porte la nouvelle école (créée sans bascule automatique).
        var reloginAuth = await client.LoginAsync(email);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", reloginAuth.AccessToken);

        return client;
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededAcademicYears()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");

        Assert.NotNull(years);
        Assert.DoesNotContain(years!, y => y.Name == "2025-2026");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededAcademicTerms()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var terms = await client.GetFromJsonAsync<List<AcademicTermDto>>("/api/academicterms");

        Assert.NotNull(terms);
        Assert.Empty(terms!);
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededPrograms()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var programs = await client.GetFromJsonAsync<List<ProgramDto>>("/api/programs");

        Assert.NotNull(programs);
        Assert.DoesNotContain(programs!, p => p.Code == "COL-GEN");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededRooms()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var rooms = await client.GetFromJsonAsync<List<RoomDto>>("/api/rooms");

        Assert.NotNull(rooms);
        Assert.DoesNotContain(rooms!, r => r.Name == "Salle 101" || r.Name == "Salle 102");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededStudentCategories()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var categories = await client.GetFromJsonAsync<List<StudentCategoryDto>>("/api/studentcategories");

        Assert.NotNull(categories);
        Assert.DoesNotContain(categories!, c => c.Name is "Standard" or "Boursier");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededStudentBatches()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var batches = await client.GetFromJsonAsync<List<StudentBatchDto>>("/api/studentbatches");

        Assert.NotNull(batches);
        Assert.DoesNotContain(batches!, b => b.Name == "Promotion 2025-2026");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededStudentGroups()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var groups = await client.GetFromJsonAsync<List<StudentGroupDto>>("/api/studentgroups");

        Assert.NotNull(groups);
        Assert.DoesNotContain(groups!, g => g.Name == "Club Sciences");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededStudents()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");

        Assert.NotNull(students);
        Assert.Empty(students!);
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededStudentApplicants()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var applicants = await client.GetFromJsonAsync<List<StudentApplicantDto>>("/api/studentapplicants");

        Assert.NotNull(applicants);
        Assert.DoesNotContain(applicants!, a => a.LastName is "Andriamanjato" or "Rasolofo" or "Rakotoson");
    }

    [Fact]
    public async Task PublicSchoolsEndpoint_ListsSeededActiveSchools()
    {
        var client = _factory.CreateClient();

        var schools = await client.GetFromJsonAsync<List<PublicSchoolDto>>("/api/schools/public");

        Assert.NotNull(schools);
        Assert.Contains(schools!, s => s.Name == "Lumière");
        Assert.Contains(schools!, s => s.Name == "Génie");
    }

    [Fact]
    public async Task PublicOpenCampaigns_AreScopedToTheRequestedSchool_NotLeakedAcrossSchools()
    {
        var director = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var schools = await director.GetFromJsonAsync<List<SchoolDto>>("/api/schools");
        var lumiere = schools!.Single(s => s.Name == "Lumière");
        var genie = schools!.Single(s => s.Name == "Génie");

        var years = await director.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);

        var createResponse = await director.PostAsJsonAsync("/api/admissioncampaigns", new CreateAdmissionCampaignRequest(
            "Campagne Isolation Test", currentYear.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30)));
        createResponse.EnsureSuccessStatusCode();
        var campaign = await createResponse.Content.ReadFromJsonAsync<AdmissionCampaignDto>();

        var anonymousClient = _factory.CreateClient();

        var openForLumiere = await anonymousClient.GetFromJsonAsync<List<OpenAdmissionCampaignDto>>(
            $"/api/admissioncampaigns/open?schoolId={lumiere.Id}");
        Assert.Contains(openForLumiere!, c => c.Id == campaign!.Id);

        var openForGenie = await anonymousClient.GetFromJsonAsync<List<OpenAdmissionCampaignDto>>(
            $"/api/admissioncampaigns/open?schoolId={genie.Id}");
        Assert.DoesNotContain(openForGenie!, c => c.Id == campaign!.Id);
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededSubjects()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var subjects = await client.GetFromJsonAsync<List<SubjectDto>>("/api/subjects");

        Assert.NotNull(subjects);
        Assert.DoesNotContain(subjects!, s => s.Name == "Mathématiques");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededCourses()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var courses = await client.GetFromJsonAsync<List<CourseDto>>("/api/courses");

        Assert.NotNull(courses);
        Assert.DoesNotContain(courses!, c => c.Name == "Mathématiques");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededCourseSchedules()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var schedules = await client.GetFromJsonAsync<List<CourseScheduleDto>>("/api/courseschedules");

        Assert.NotNull(schedules);
        Assert.Empty(schedules!);
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededProgramEnrollments()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var enrollments = await client.GetFromJsonAsync<List<ProgramEnrollmentDto>>("/api/programenrollments");

        Assert.NotNull(enrollments);
        Assert.Empty(enrollments!);
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededCourseEnrollments()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var enrollments = await client.GetFromJsonAsync<List<CourseEnrollmentDto>>("/api/courseenrollments");

        Assert.NotNull(enrollments);
        Assert.Empty(enrollments!);
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededLeaveApplications()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var applications = await client.GetFromJsonAsync<List<LeaveApplicationDto>>("/api/leaveapplications");

        Assert.NotNull(applications);
        Assert.Empty(applications!);
    }

    [Fact]
    public async Task NewSchool_DirectorCannotSeeLumieresAttendance_ByGuessingItsClassId()
    {
        // Attendance n'a pas sa propre colonne SchoolId (scopée via Class, comme Student) : ce test vérifie
        // spécifiquement que le filtre à un niveau protège bien même quand CanAccessClassAsync laisse
        // passer n'importe quel Directeur sans vérifier que la classe appartient à son école active.
        var lumiereDirector = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var lumiereStudents = await lumiereDirector.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var lumiereClassId = lumiereStudents!.First().ClassId;

        var client = await RegisterDirectorWithFreshSchoolAsync();

        var response = await client.GetAsync($"/api/attendance?classId={lumiereClassId}&date={DateTime.UtcNow:O}");
        response.EnsureSuccessStatusCode();
        var records = await response.Content.ReadFromJsonAsync<List<AttendanceDto>>();

        Assert.NotNull(records);
        Assert.Empty(records!);
    }

    [Fact]
    public async Task GenieSchool_OnlySeesItsOwnIsolatedClass_NotLumieresStudents()
    {
        // Le directeur bascule sur Génie : la classe "3ème C" y est isolée et n'a aucun élève,
        // alors que les 8 élèves seedés appartiennent tous à des classes de Lumière.
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var schools = await client.GetFromJsonAsync<List<GestionScolaire.Application.DTOs.Schools.SchoolDto>>("/api/schools");
        var genie = schools!.Single(s => s.Name == "Génie");

        var switchResponse = await client.PostAsJsonAsync("/api/auth/switch-school", new SwitchSchoolRequest(genie.Id));
        var switched = await switchResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", switched!.AccessToken);

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        Assert.NotNull(students);
        Assert.Empty(students!);

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        Assert.DoesNotContain(years!, y => y.IsCurrent);

        // Remet le directeur sur sa première école pour ne pas affecter les autres tests partageant la base.
        var lumiere = schools!.Single(s => s.Name == "Lumière");
        await client.PostAsJsonAsync("/api/auth/switch-school", new SwitchSchoolRequest(lumiere.Id));
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededGradingScales()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var scales = await client.GetFromJsonAsync<List<GradingScaleDto>>("/api/gradingscales");

        Assert.NotNull(scales);
        Assert.DoesNotContain(scales!, s => s.Name == "Barème standard");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededAssessmentGroups()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var groups = await client.GetFromJsonAsync<List<AssessmentGroupDto>>("/api/assessmentgroups");

        Assert.NotNull(groups);
        Assert.DoesNotContain(groups!, g => g.Name is "Devoirs" or "Compositions");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededAssessmentPlans()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var plans = await client.GetFromJsonAsync<List<AssessmentPlanDto>>("/api/assessmentplans");

        Assert.NotNull(plans);
        Assert.Empty(plans!);
    }

    [Fact]
    public async Task NewSchool_DirectorCannotSeeLumieresGrades_ByGuessingItsStudentId()
    {
        // Grade n'a pas sa propre colonne SchoolId (scopée via Class, comme Student/Attendance) : ce test
        // vérifie spécifiquement que le filtre à un niveau protège bien même quand IStudentAccessPolicy
        // laisse passer n'importe quel Directeur sans vérifier que l'élève appartient à son école active
        // (c'est précisément la vulnérabilité corrigée en Phase 5 dans GetByStudent : un IgnoreQueryFilters()
        // devenu inconditionnel combiné à ce laisser-passer permettait de fuiter les notes d'une autre école).
        var lumiereDirector = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var lumiereStudents = await lumiereDirector.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var lumiereStudentId = lumiereStudents!.First().Id;

        var client = await RegisterDirectorWithFreshSchoolAsync();

        var response = await client.GetAsync($"/api/grades/student/{lumiereStudentId}");
        response.EnsureSuccessStatusCode();
        var grades = await response.Content.ReadFromJsonAsync<List<GradeDto>>();

        Assert.NotNull(grades);
        Assert.Empty(grades!);
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededFeeCategories()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var categories = await client.GetFromJsonAsync<List<FeeCategoryDto>>("/api/feecategories");

        Assert.NotNull(categories);
        Assert.DoesNotContain(categories!, c => c.Name is "Scolarité" or "Cantine" or "Transport");
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededFeeStructures()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var structures = await client.GetFromJsonAsync<List<FeeStructureDto>>("/api/feestructures");

        Assert.NotNull(structures);
        Assert.DoesNotContain(structures!, s => s.Name == "Frais standard 2025-2026");
    }

    [Fact]
    public async Task NewSchool_DirectorCannotSeeLumieresInvoices_ByGuessingItsStudentId()
    {
        // Invoice a désormais sa propre colonne SchoolId (aucun ancrage fiable à un seul niveau
        // n'existait : Student et FeeSchedule sont tous deux à 2 sauts d'une vraie colonne SchoolId).
        // Ce test vérifie que le filtre protège bien même quand IStudentAccessPolicy laisse passer
        // n'importe quel Directeur sans vérifier l'école — la vulnérabilité corrigée en Phase 5.
        var lumiereDirector = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var lumiereStudents = await lumiereDirector.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var lumiereStudentId = lumiereStudents!.First().Id;

        var client = await RegisterDirectorWithFreshSchoolAsync();

        var response = await client.GetAsync($"/api/invoices/student/{lumiereStudentId}");
        response.EnsureSuccessStatusCode();
        var invoices = await response.Content.ReadFromJsonAsync<List<InvoiceDto>>();

        Assert.NotNull(invoices);
        Assert.Empty(invoices!);
    }

    [Fact]
    public async Task NewSchool_DirectorCannotSeeLumieresPayments_ByGuessingItsStudentId()
    {
        var lumiereDirector = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var lumiereStudents = await lumiereDirector.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var lumiereStudentId = lumiereStudents!.First().Id;

        var client = await RegisterDirectorWithFreshSchoolAsync();

        var response = await client.GetAsync($"/api/payments/student/{lumiereStudentId}");
        response.EnsureSuccessStatusCode();
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>();

        Assert.NotNull(payments);
        Assert.Empty(payments!);
    }

    [Fact]
    public async Task NewSchool_NeverSeesSeededGuardians()
    {
        var client = await RegisterDirectorWithFreshSchoolAsync();

        var guardians = await client.GetFromJsonAsync<List<GuardianDto>>("/api/guardians");

        Assert.NotNull(guardians);
        Assert.DoesNotContain(guardians!, g => g.LastName is "Randria" or "Rasoanaivo");
    }

    [Fact]
    public async Task NewSchool_DirectorCannotSeeLumieresGuardians_ByGuessingItsStudentId()
    {
        // StudentGuardian n'a pas sa propre colonne SchoolId (scopée via Guardian, qui en a une propre) :
        // ce test vérifie que le filtre à un niveau protège bien même quand IStudentAccessPolicy laisse
        // passer n'importe quel Directeur. Avant cette phase, GuardiansController.GetByStudent n'avait
        // aucun filtre du tout sur StudentGuardian — la fuite était donc déjà possible, corrigée ici.
        var lumiereDirector = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var lumiereStudents = await lumiereDirector.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var lumiereStudentId = lumiereStudents!.First().Id;

        var client = await RegisterDirectorWithFreshSchoolAsync();

        var response = await client.GetAsync($"/api/guardians/student/{lumiereStudentId}");
        response.EnsureSuccessStatusCode();
        var links = await response.Content.ReadFromJsonAsync<List<StudentGuardianDto>>();

        Assert.NotNull(links);
        Assert.Empty(links!);
    }

    [Fact]
    public async Task NewSchool_DirectorCannotSeeLumieresSiblings_ByGuessingItsStudentId()
    {
        // StudentSibling n'a ni colonne SchoolId ni filtre propre : la protection vient entièrement du
        // filtre de Student (via Class) appliqué aux deux navigations Student/SiblingStudent. Avant cette
        // phase, StudentsController.GetSiblings appelait .IgnoreQueryFilters() sans condition, ce qui
        // désactivait cette protection pour tout le monde, Directeur d'une autre école y compris.
        var lumiereDirector = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var lumiereStudents = await lumiereDirector.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var lumiereStudentId = lumiereStudents!.First().Id;

        var client = await RegisterDirectorWithFreshSchoolAsync();

        var response = await client.GetAsync($"/api/students/{lumiereStudentId}/siblings");
        response.EnsureSuccessStatusCode();
        var siblings = await response.Content.ReadFromJsonAsync<List<SiblingDto>>();

        Assert.NotNull(siblings);
        Assert.Empty(siblings!);
    }

    [Fact]
    public async Task NewSchool_DirectorCannotSeeLumieresStudentLogs_ByGuessingItsStudentId()
    {
        var lumiereDirector = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var lumiereStudents = await lumiereDirector.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var lumiereStudentId = lumiereStudents!.First().Id;

        var client = await RegisterDirectorWithFreshSchoolAsync();

        var response = await client.GetAsync($"/api/studentlogs/student/{lumiereStudentId}");
        response.EnsureSuccessStatusCode();
        var logs = await response.Content.ReadFromJsonAsync<List<StudentLogDto>>();

        Assert.NotNull(logs);
        Assert.Empty(logs!);
    }
}

using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.AcademicTerms;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.Auth;
using GestionScolaire.Application.DTOs.Programs;
using GestionScolaire.Application.DTOs.Rooms;
using GestionScolaire.Application.DTOs.Students;
using GestionScolaire.Application.DTOs.StudentBatches;
using GestionScolaire.Application.DTOs.StudentCategories;
using GestionScolaire.Application.DTOs.StudentGroups;
using Xunit;

namespace GestionScolaire.Api.Tests;

/// Phase 1 : AcademicYear, AcademicTerm, AcademicProgram, Room, StudentCategory, StudentBatch, StudentGroup
/// et Student (via Class) sont désormais scopés par école. Ces tests vérifient qu'un directeur tout juste
/// inscrit, propriétaire d'une école fraîchement créée, ne voit jamais les données déjà seedées pour
/// Lumière/Génie — c'est la garantie de sécurité la plus importante de cette phase.
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
}

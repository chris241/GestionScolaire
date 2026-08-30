using System.Net;
using System.Net.Http.Json;
using System.Text;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class StudentsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public StudentsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/students");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Director_SeesAllEightStudents()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");

        Assert.NotNull(students);
        Assert.Equal(8, students!.Count);
    }

    [Fact]
    public async Task Parent_OnlySeesOwnChild()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");

        Assert.NotNull(students);
        Assert.Single(students!);
    }

    [Fact]
    public async Task Teacher_OnlySeesOwnHomeroomClassStudents()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");

        Assert.NotNull(students);
        Assert.NotEmpty(students!);
        // Tous les élèves renvoyés doivent appartenir à la même classe (celle du professeur).
        Assert.Single(students!.Select(s => s.ClassName).Distinct());
    }

    [Fact]
    public async Task TwoTeachers_SeeDisjointSetsOfStudents()
    {
        var mathClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var mathStudents = await mathClient.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var frenchStudents = await frenchClient.GetFromJsonAsync<List<StudentDto>>("/api/students");

        var mathIds = mathStudents!.Select(s => s.Id).ToHashSet();
        var frenchIds = frenchStudents!.Select(s => s.Id).ToHashSet();

        Assert.Empty(mathIds.Intersect(frenchIds));
    }

    [Fact]
    public async Task Director_SeesSeededSiblingLink_Bidirectionally()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var tojo = students!.Single(s => s.EnrollmentNumber == "MAT-2026-001");
        var sitraka = students!.Single(s => s.EnrollmentNumber == "MAT-2026-006");

        var tojoSiblings = await client.GetFromJsonAsync<List<SiblingDto>>($"/api/students/{tojo.Id}/siblings");
        var sitrakaSiblings = await client.GetFromJsonAsync<List<SiblingDto>>($"/api/students/{sitraka.Id}/siblings");

        Assert.Contains(tojoSiblings!, s => s.StudentId == sitraka.Id);
        Assert.Contains(sitrakaSiblings!, s => s.StudentId == tojo.Id);
    }

    [Fact]
    public async Task Director_CanAddAndRemoveSibling_BetweenTwoStudents()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var a = students!.Single(s => s.EnrollmentNumber == "MAT-2026-003");
        var b = students!.Single(s => s.EnrollmentNumber == "MAT-2026-004");

        var addResponse = await client.PostAsync($"/api/students/{a.Id}/siblings/{b.Id}", null);
        Assert.Equal(HttpStatusCode.NoContent, addResponse.StatusCode);

        var aSiblings = await client.GetFromJsonAsync<List<SiblingDto>>($"/api/students/{a.Id}/siblings");
        Assert.Contains(aSiblings!, s => s.StudentId == b.Id);

        var removeResponse = await client.DeleteAsync($"/api/students/{a.Id}/siblings/{b.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var aSiblingsAfter = await client.GetFromJsonAsync<List<SiblingDto>>($"/api/students/{a.Id}/siblings");
        Assert.DoesNotContain(aSiblingsAfter!, s => s.StudentId == b.Id);
    }

    [Fact]
    public async Task Director_CannotAddStudentAsOwnSibling()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var a = students!.First();

        var response = await client.PostAsync($"/api/students/{a.Id}/siblings/{a.Id}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotAddSibling()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var a = students!.First();
        var b = students!.Last();

        var response = await client.PostAsync($"/api/students/{a.Id}/siblings/{b.Id}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CanViewOwnChildSiblings_ButNotOtherChild()
    {
        var parent1 = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2 = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var ownChild = (await parent1.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();
        var otherChild = (await parent2.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var ownResponse = await parent1.GetAsync($"/api/students/{ownChild.Id}/siblings");
        ownResponse.EnsureSuccessStatusCode();

        var otherResponse = await parent1.GetAsync($"/api/students/{otherChild.Id}/siblings");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    // Ne teste volontairement que les lignes en échec : la suite partage une seule base Postgres entre
    // toutes les classes de test (exécution séquentielle), et un élève réellement créé ici fausserait les
    // décomptes déjà fixés ailleurs (ex. FeeCollectionReportsEndpointsTests, AttendanceEndpointsTests).
    // Le chemin de création réussie réutilise le même schéma que StudentApplicantsController.Accept
    // (déjà couvert par ses propres tests) et a été vérifié manuellement via Docker/curl.
    [Fact]
    public async Task Director_ImportStudentsFromCsv_ReportsEachInvalidRowWithItsReason()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var csv = "FirstName,LastName,DateOfBirth,Gender,ClassName,EnrollmentNumber\n" +
                   "Lala,Ranaivo,2012-09-01,Masculin,ClasseInexistante,\n" +
                   "Tiana,Rakoto,pas-une-date,Masculin,6ème A,\n" +
                   "Voa,Randria,2012-01-01,PasUnGenre,6ème A,\n" +
                   ",Sans Prenom,2012-01-01,Masculin,6ème A,\n" +
                   "Trop,Court,2012-01-01\n";

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, Encoding.UTF8), "file", "import.csv");

        var response = await client.PostAsync("/api/students/import", content);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<StudentImportResultDto>();

        Assert.Equal(5, result!.TotalRows);
        Assert.Equal(0, result.SuccessCount);
        Assert.Equal(5, result.ErrorCount);
        Assert.Contains(result.Rows, r => r.FirstName == "Lala" && !r.Success && r.ErrorMessage!.Contains("Classe introuvable"));
        Assert.Contains(result.Rows, r => r.FirstName == "Tiana" && !r.Success && r.ErrorMessage!.Contains("Date de naissance"));
        Assert.Contains(result.Rows, r => r.FirstName == "Voa" && !r.Success && r.ErrorMessage!.Contains("Genre invalide"));
        Assert.Contains(result.Rows, r => r.LastName == "Sans Prenom" && !r.Success && r.ErrorMessage!.Contains("Prénom et nom"));
        Assert.Contains(result.Rows, r => r.LastName == "Court" && !r.Success && r.ErrorMessage!.Contains("incomplète"));
    }

    [Fact]
    public async Task Teacher_CannotImportStudents()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var csv = "FirstName,LastName,DateOfBirth,Gender,ClassName,EnrollmentNumber\nInterdit,Test,2013-01-01,Masculin,6ème A,\n";
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(csv, Encoding.UTF8), "file", "import.csv");

        var response = await client.PostAsync("/api/students/import", content);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

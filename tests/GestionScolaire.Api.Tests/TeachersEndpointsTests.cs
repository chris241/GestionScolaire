using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Teachers;
using GestionScolaire.Application.DTOs.Auth;
using GestionScolaire.Application.DTOs.Schools;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class TeachersEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public TeachersEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededTeachers()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");

        Assert.NotNull(teachers);
        Assert.Contains(teachers!, t => t.Email == "prof.math@ecole.mg");
    }

    [Fact]
    public async Task Director_CanCreateTeacher_AndLogInAsThem()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var createResponse = await client.PostAsJsonAsync("/api/teachers", new CreateTeacherRequest(
            "Nouveau", "Professeur", "nouveau.prof@ecole.mg", "Password123!", "Physique", new DateTime(2026, 1, 1)));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<TeacherDto>();

        Assert.Equal("Nouveau Professeur", created!.FullName);
        Assert.Equal("Physique", created.Specialty);

        // Vérifie que le lien TeacherSchool a bien été créé : sans lui, l'enseignant serait filtré
        // partout (invisible dans GetAll) et son token n'aurait aucune école active.
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        Assert.Contains(teachers!, t => t.Email == "nouveau.prof@ecole.mg");

        var loginClient = _factory.CreateClient();
        var auth = await loginClient.LoginAsync("nouveau.prof@ecole.mg", "Password123!");
        Assert.NotNull(auth.AccessToken);
        Assert.NotNull(auth.User.ActiveSchoolId);
        Assert.Equal("Lumière", auth.User.ActiveSchoolName);
    }

    [Fact]
    public async Task Create_RejectsDuplicateEmail()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/teachers", new CreateTeacherRequest(
            "Doublon", "Test", "prof.math@ecole.mg", "Password123!", "Maths", new DateTime(2026, 1, 1)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotCreateTeacher()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/teachers", new CreateTeacherRequest(
            "Interdit", "Test", "interdit@ecole.mg", "Password123!", "Maths", new DateTime(2026, 1, 1)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Director_CanLinkExistingTeacherToAnotherSchool_AndUnlink()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var schools = await client.GetFromJsonAsync<List<SchoolDto>>("/api/schools");
        var genie = schools!.Single(s => s.Name == "Génie");

        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var mathTeacher = teachers!.Single(t => t.Email == "prof.math@ecole.mg");
        Assert.Single(mathTeacher.Schools);

        var linkResponse = await client.PostAsync($"/api/teachers/{mathTeacher.Id}/schools/{genie.Id}", null);
        linkResponse.EnsureSuccessStatusCode();
        var linked = await linkResponse.Content.ReadFromJsonAsync<TeacherDto>();
        Assert.Equal(2, linked!.Schools.Count);
        Assert.Contains(linked.Schools, s => s.Name == "Génie");

        // Impossible de retirer la dernière école, mais retirer l'une des deux doit fonctionner.
        var unlinkResponse = await client.DeleteAsync($"/api/teachers/{mathTeacher.Id}/schools/{genie.Id}");
        Assert.Equal(HttpStatusCode.NoContent, unlinkResponse.StatusCode);

        var afterUnlink = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        Assert.Single(afterUnlink!.Single(t => t.Id == mathTeacher.Id).Schools);
    }

    [Fact]
    public async Task Director_CannotUnlinkTeachersLastSchool()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var mathTeacher = teachers!.Single(t => t.Email == "prof.math@ecole.mg");
        var onlySchoolId = mathTeacher.Schools.Single().Id;

        var response = await client.DeleteAsync($"/api/teachers/{mathTeacher.Id}/schools/{onlySchoolId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Director_CannotLinkTeacherToAnotherDirectorsSchool()
    {
        // Le professeur de maths de Lumière n'est pas visible pour un directeur fraîchement inscrit
        // (filtré, puisqu'il n'a aucun lien TeacherSchool vers l'école active de ce directeur) : la
        // tentative de rattachement échoue avec 404, pas une fuite de données cross-tenant.
        var lumiereClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var teachers = await lumiereClient.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        var mathTeacherId = teachers!.Single(t => t.Email == "prof.math@ecole.mg").Id;

        var email = $"isole.teacherlink.{Guid.NewGuid():N}@ecole.mg";
        var freshClient = _factory.CreateClient();
        await freshClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, AuthHelper.DemoPassword, "Isole", "Directeur", "Director"));
        var authedClient = await freshClient.AsUserAsync(email);
        await authedClient.PostAsJsonAsync("/api/schools", new CreateSchoolRequest("École Isolée", null, "MGA", 20));
        var relogin = await freshClient.LoginAsync(email);
        freshClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", relogin.AccessToken);
        var freshSchools = await freshClient.GetFromJsonAsync<List<SchoolDto>>("/api/schools");
        var freshSchoolId = freshSchools!.Single().Id;

        var response = await freshClient.PostAsync($"/api/teachers/{mathTeacherId}/schools/{freshSchoolId}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

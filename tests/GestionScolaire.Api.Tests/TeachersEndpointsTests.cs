using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Teachers;
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

        var loginClient = _factory.CreateClient();
        var auth = await loginClient.LoginAsync("nouveau.prof@ecole.mg", "Password123!");
        Assert.NotNull(auth.AccessToken);
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
}

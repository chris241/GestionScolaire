using System.Net;
using System.Net.Http.Json;
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
}

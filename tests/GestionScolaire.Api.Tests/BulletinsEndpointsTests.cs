using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class BulletinsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public BulletinsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Parent_CanDownloadOwnChildBulletin()
    {
        var parentClient = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var ownChild = (await parentClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var response = await parentClient.GetAsync($"/api/bulletins/student/{ownChild.Id}?term=Trimestre 1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        // Un PDF valide commence toujours par la signature %PDF-.
        Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [Fact]
    public async Task Parent_CannotDownloadAnotherChildBulletin()
    {
        var parent1Client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2Client = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var otherChild = (await parent2Client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var response = await parent1Client.GetAsync($"/api/bulletins/student/{otherChild.Id}?term=Trimestre 1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CanDownloadOwnStudentBulletin()
    {
        var teacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var ownStudent = (await teacherClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First();

        var response = await teacherClient.GetAsync($"/api/bulletins/student/{ownStudent.Id}?term=Trimestre 1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotDownloadOtherClassStudentBulletin()
    {
        var mathTeacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchTeacherClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var otherClassStudent = (await frenchTeacherClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First();

        var response = await mathTeacherClient.GetAsync($"/api/bulletins/student/{otherClassStudent.Id}?term=Trimestre 1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CanDownloadZipOfAllBulletins_ForOwnClass()
    {
        var teacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var ownClassId = (await teacherClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First().ClassId;

        var response = await teacherClient.GetAsync($"/api/bulletins/class/{ownClassId}?term=Trimestre 1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        // Un ZIP valide commence toujours par la signature PK\x03\x04.
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);
    }

    [Fact]
    public async Task Teacher_CannotDownloadZipOfAllBulletins_ForOtherClass()
    {
        var mathTeacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchTeacherClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var otherClassId = (await frenchTeacherClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First().ClassId;

        var response = await mathTeacherClient.GetAsync($"/api/bulletins/class/{otherClassId}?term=Trimestre 1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CannotDownloadZipOfAllBulletins()
    {
        var parentClient = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var classId = (await parentClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single().ClassId;

        var response = await parentClient.GetAsync($"/api/bulletins/class/{classId}?term=Trimestre 1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

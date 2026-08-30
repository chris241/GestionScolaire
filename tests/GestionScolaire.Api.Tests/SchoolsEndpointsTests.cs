using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Auth;
using GestionScolaire.Application.DTOs.Schools;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class SchoolsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public SchoolsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> RegisterDirectorAsync(string email)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, AuthHelper.DemoPassword, "Nouveau", "Directeur", "Director"));
        registerResponse.EnsureSuccessStatusCode();

        return await client.AsUserAsync(email);
    }

    [Fact]
    public async Task Director_SeesOnlyOwnSchools()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var schools = await client.GetFromJsonAsync<List<SchoolDto>>("/api/schools");

        Assert.NotNull(schools);
        Assert.Equal(2, schools!.Count);
        Assert.Contains(schools, s => s.Name == "Lumière");
        Assert.Contains(schools, s => s.Name == "Génie");
    }

    [Fact]
    public async Task Director_CanCreateAndUpdateSchool()
    {
        var email = $"nouveau.directeur.{Guid.NewGuid():N}@ecole.mg";
        var client = await RegisterDirectorAsync(email);

        var createResponse = await client.PostAsJsonAsync("/api/schools", new CreateSchoolRequest(
            "École Test", "Toamasina", "MGA", 20));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<SchoolDto>();
        Assert.Equal("École Test", created!.Name);

        var updateResponse = await client.PutAsJsonAsync($"/api/schools/{created.Id}", new UpdateSchoolRequest(
            "École Test Renommée", "Toamasina", "MGA", 20));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<SchoolDto>();
        Assert.Equal("École Test Renommée", updated!.Name);
    }

    [Fact]
    public async Task Director_CannotUpdateAnotherDirectorsSchool()
    {
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var schools = await directorClient.GetFromJsonAsync<List<SchoolDto>>("/api/schools");
        var otherDirectorsSchool = schools!.First(s => s.Name == "Lumière");

        var otherEmail = $"intrus.{Guid.NewGuid():N}@ecole.mg";
        var intruderClient = await RegisterDirectorAsync(otherEmail);

        var response = await intruderClient.PutAsJsonAsync($"/api/schools/{otherDirectorsSchool.Id}", new UpdateSchoolRequest(
            "Piraté", null, "MGA", 20));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotCreateSchool()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/schools", new CreateSchoolRequest(
            "École Interdite", null, "MGA", 20));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_SeesOnlySchoolsTheyAreLinkedTo()
    {
        var mathTeacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var frenchTeacherClient = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var mathSchools = await mathTeacherClient.GetFromJsonAsync<List<SchoolDto>>("/api/schools");
        var frenchSchools = await frenchTeacherClient.GetFromJsonAsync<List<SchoolDto>>("/api/schools");

        Assert.Single(mathSchools!);
        Assert.Equal("Lumière", mathSchools!.Single().Name);

        Assert.Equal(2, frenchSchools!.Count);
    }
}

using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Guardians;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class GuardiansEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public GuardiansEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Director_SeesSeededGuardians()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var guardians = await client.GetFromJsonAsync<List<GuardianDto>>("/api/guardians");

        Assert.NotNull(guardians);
        Assert.Contains(guardians!, g => g.FullName == "Herizo Randria");
        Assert.Contains(guardians!, g => g.FullName == "Voninavoko Rasoanaivo");
    }

    [Fact]
    public async Task Director_SeesSharedGuardian_OnBothSiblings()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var tojo = students!.Single(s => s.EnrollmentNumber == "MAT-2026-001");
        var sitraka = students!.Single(s => s.EnrollmentNumber == "MAT-2026-006");

        var tojoGuardians = await client.GetFromJsonAsync<List<StudentGuardianDto>>($"/api/guardians/student/{tojo.Id}");
        var sitrakaGuardians = await client.GetFromJsonAsync<List<StudentGuardianDto>>($"/api/guardians/student/{sitraka.Id}");

        Assert.Contains(tojoGuardians!, g => g.GuardianFullName == "Herizo Randria" && g.IsPrimaryContact);
        Assert.Contains(sitrakaGuardians!, g => g.GuardianFullName == "Herizo Randria" && g.IsPrimaryContact);
    }

    [Fact]
    public async Task Director_CanCreateGuardian_LinkToStudent_AndUnlink()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var target = students!.Single(s => s.EnrollmentNumber == "MAT-2026-002");

        var createResponse = await client.PostAsJsonAsync("/api/guardians", new CreateGuardianRequest(
            "Test", "Tuteur", "034 00 000 00", null, null, null));
        createResponse.EnsureSuccessStatusCode();
        var guardian = await createResponse.Content.ReadFromJsonAsync<GuardianDto>();

        var linkResponse = await client.PostAsJsonAsync($"/api/guardians/{guardian!.Id}/students/{target.Id}",
            new LinkGuardianRequest("Oncle", false));
        linkResponse.EnsureSuccessStatusCode();

        var afterLink = await client.GetFromJsonAsync<List<StudentGuardianDto>>($"/api/guardians/student/{target.Id}");
        Assert.Contains(afterLink!, l => l.GuardianId == guardian.Id);

        var duplicateLinkResponse = await client.PostAsJsonAsync($"/api/guardians/{guardian.Id}/students/{target.Id}",
            new LinkGuardianRequest("Oncle", false));
        Assert.Equal(HttpStatusCode.Conflict, duplicateLinkResponse.StatusCode);

        var unlinkResponse = await client.DeleteAsync($"/api/guardians/{guardian.Id}/students/{target.Id}");
        Assert.Equal(HttpStatusCode.NoContent, unlinkResponse.StatusCode);

        var afterUnlink = await client.GetFromJsonAsync<List<StudentGuardianDto>>($"/api/guardians/student/{target.Id}");
        Assert.DoesNotContain(afterUnlink!, l => l.GuardianId == guardian.Id);
    }

    [Fact]
    public async Task Director_CanSetAndClearGuardianInterests()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var createResponse = await client.PostAsJsonAsync("/api/guardians", new CreateGuardianRequest(
            "Interet", "Tuteur", "034 00 000 00", null, null, null));
        var guardian = await createResponse.Content.ReadFromJsonAsync<GuardianDto>();
        Assert.Null(guardian!.AreasOfInterest);

        var updateResponse = await client.PutAsJsonAsync($"/api/guardians/{guardian.Id}/interests",
            new UpdateGuardianInterestsRequest("Bénévolat, comité des fêtes"));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<GuardianDto>();
        Assert.Equal("Bénévolat, comité des fêtes", updated!.AreasOfInterest);

        var clearResponse = await client.PutAsJsonAsync($"/api/guardians/{guardian.Id}/interests",
            new UpdateGuardianInterestsRequest(null));
        clearResponse.EnsureSuccessStatusCode();
        var cleared = await clearResponse.Content.ReadFromJsonAsync<GuardianDto>();
        Assert.Null(cleared!.AreasOfInterest);
    }

    [Fact]
    public async Task Teacher_CannotCreateGuardian()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/guardians", new CreateGuardianRequest(
            "Interdit", "Test", "034 00 000 00", null, null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CanViewOwnChildGuardians_ButNotOtherChild()
    {
        var parent1 = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2 = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var ownChild = (await parent1.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();
        var otherChild = (await parent2.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var ownResponse = await parent1.GetAsync($"/api/guardians/student/{ownChild.Id}");
        ownResponse.EnsureSuccessStatusCode();

        var otherResponse = await parent1.GetAsync($"/api/guardians/student/{otherChild.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Student_CanViewOwnGuardians()
    {
        var client = await _factory.CreateClient().AsUserAsync("eleve1@ecole.mg");
        var self = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var response = await client.GetAsync($"/api/guardians/student/{self.Id}");

        response.EnsureSuccessStatusCode();
        var guardians = await response.Content.ReadFromJsonAsync<List<StudentGuardianDto>>();
        Assert.Contains(guardians!, g => g.GuardianFullName == "Herizo Randria");
    }
}

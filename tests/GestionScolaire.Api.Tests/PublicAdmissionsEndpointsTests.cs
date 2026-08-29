using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Admissions;
using GestionScolaire.Domain.Enums;
using Xunit;

namespace GestionScolaire.Api.Tests;

/// Le endpoint public est limité en débit par IP (voir Program.cs) ; ce fichier garde volontairement
/// un nombre de requêtes minimal vers /api/studentapplicants/public pour ne jamais déclencher le 429
/// pendant une exécution normale de la suite.
[Collection(ApiTestCollection.Name)]
public class PublicAdmissionsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public PublicAdmissionsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Public_CanSubmitApplication_WithoutAuthentication_AndAppearsInDirectorList()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync("/api/studentapplicants/public", new PublicApplicantRequest(
            "Candidat", "PortailPublic", new DateTime(2014, 4, 12), Gender.Masculin,
            null, null, "Parent DePortailPublic", null, "034 55 555 55", "5ème"));

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<StudentApplicantDto>();

        Assert.Equal("Submitted", created!.Status);
        Assert.NotEqual(Guid.Empty, created.AcademicYearId);
        Assert.False(string.IsNullOrEmpty(created.AcademicYearName));

        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var applicants = await directorClient.GetFromJsonAsync<List<StudentApplicantDto>>("/api/studentapplicants");

        Assert.Contains(applicants!, a => a.Id == created.Id);
    }

    [Fact]
    public async Task Public_RejectsSubmission_WithoutGuardianContact()
    {
        var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.PostAsync("/api/studentapplicants/public", JsonContent.Create(new
        {
            FirstName = "Sans",
            LastName = "Tuteur",
            DateOfBirth = new DateTime(2014, 4, 12),
            Gender = 1,
            LevelAppliedFor = "5ème"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

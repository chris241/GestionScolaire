using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Admissions;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.Programs;
using GestionScolaire.Domain.Enums;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class AdmissionCampaignsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AdmissionCampaignsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<(Guid YearId, Guid ProgramId)> GetSeedIdsAsync(HttpClient client)
    {
        var year = (await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears"))!.Single(y => y.IsCurrent);
        var program = (await client.GetFromJsonAsync<List<ProgramDto>>("/api/programs"))!.First();
        return (year.Id, program.Id);
    }

    [Fact]
    public async Task Director_CanCreateCampaign_SetQuota_AndSeeItInOpenList()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var (yearId, programId) = await GetSeedIdsAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/admissioncampaigns", new CreateAdmissionCampaignRequest(
            "Campagne Test Ouverte", yearId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30)));
        createResponse.EnsureSuccessStatusCode();
        var campaign = await createResponse.Content.ReadFromJsonAsync<AdmissionCampaignDto>();
        Assert.True(campaign!.IsOpen);

        var quotaResponse = await client.PostAsJsonAsync($"/api/admissioncampaigns/{campaign.Id}/quotas",
            new SetCampaignQuotaRequest(programId, 1));
        quotaResponse.EnsureSuccessStatusCode();
        var withQuota = await quotaResponse.Content.ReadFromJsonAsync<AdmissionCampaignDto>();
        var quota = Assert.Single(withQuota!.Quotas);
        Assert.Equal(1, quota.Quota);
        Assert.Equal(0, quota.Used);
        Assert.Equal(1, quota.Remaining);

        var openList = await client.GetFromJsonAsync<List<OpenAdmissionCampaignDto>>("/api/admissioncampaigns/open");
        var openEntry = Assert.Single(openList!, c => c.Id == campaign.Id);
        Assert.Contains(openEntry.Programs, p => p.Id == programId);
    }

    [Fact]
    public async Task ClosedCampaign_DoesNotAppearInOpenList_AndRejectsSubmissions()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var (yearId, _) = await GetSeedIdsAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/admissioncampaigns", new CreateAdmissionCampaignRequest(
            "Campagne Test Fermée", yearId, DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-30)));
        var campaign = await createResponse.Content.ReadFromJsonAsync<AdmissionCampaignDto>();
        Assert.False(campaign!.IsOpen);

        var openList = await client.GetFromJsonAsync<List<OpenAdmissionCampaignDto>>("/api/admissioncampaigns/open");
        Assert.DoesNotContain(openList!, c => c.Id == campaign.Id);

        var applyResponse = await client.PostAsJsonAsync("/api/studentapplicants", new CreateStudentApplicantRequest(
            "Trop", "Tard", new DateTime(2014, 5, 1), Gender.Masculin,
            null, null, null, null, null, "6ème", yearId, null, campaign.Id));

        Assert.Equal(HttpStatusCode.BadRequest, applyResponse.StatusCode);
    }

    [Fact]
    public async Task Accept_EnforcesQuota_AndRejectsOnceExhausted()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var (yearId, programId) = await GetSeedIdsAsync(client);
        var students = await client.GetFromJsonAsync<List<GestionScolaire.Application.DTOs.Students.StudentDto>>("/api/students");
        var classId = students!.First().ClassId;

        var createCampaignResponse = await client.PostAsJsonAsync("/api/admissioncampaigns", new CreateAdmissionCampaignRequest(
            "Campagne Quota", yearId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30)));
        var campaign = await createCampaignResponse.Content.ReadFromJsonAsync<AdmissionCampaignDto>();

        await client.PostAsJsonAsync($"/api/admissioncampaigns/{campaign!.Id}/quotas", new SetCampaignQuotaRequest(programId, 1));

        async Task<Guid> CreateApplicantAsync(string firstName)
        {
            var response = await client.PostAsJsonAsync("/api/studentapplicants", new CreateStudentApplicantRequest(
                firstName, "Quota", new DateTime(2014, 5, 1), Gender.Masculin,
                null, null, null, null, null, "6ème", yearId, programId, campaign.Id));
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<StudentApplicantDto>();
            return dto!.Id;
        }

        var firstApplicantId = await CreateApplicantAsync("Premier");
        var secondApplicantId = await CreateApplicantAsync("Second");

        var firstAcceptResponse = await client.PostAsJsonAsync($"/api/studentapplicants/{firstApplicantId}/accept",
            new AcceptApplicantRequest(classId, null));
        firstAcceptResponse.EnsureSuccessStatusCode();

        var secondAcceptResponse = await client.PostAsJsonAsync($"/api/studentapplicants/{secondApplicantId}/accept",
            new AcceptApplicantRequest(classId, null));
        Assert.Equal(HttpStatusCode.Conflict, secondAcceptResponse.StatusCode);
    }

    [Fact]
    public async Task Director_CannotDeleteCampaign_WithApplicants()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var (yearId, _) = await GetSeedIdsAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/admissioncampaigns", new CreateAdmissionCampaignRequest(
            "Campagne À Protéger", yearId, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(30)));
        var campaign = await createResponse.Content.ReadFromJsonAsync<AdmissionCampaignDto>();

        await client.PostAsJsonAsync("/api/studentapplicants", new CreateStudentApplicantRequest(
            "Protège", "Moi", new DateTime(2014, 5, 1), Gender.Masculin,
            null, null, null, null, null, "6ème", yearId, null, campaign!.Id));

        var deleteResponse = await client.DeleteAsync($"/api/admissioncampaigns/{campaign.Id}");

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotManageCampaigns()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var director = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var (yearId, _) = await GetSeedIdsAsync(director);

        var response = await client.PostAsJsonAsync("/api/admissioncampaigns", new CreateAdmissionCampaignRequest(
            "Interdit", yearId, DateTime.UtcNow, DateTime.UtcNow.AddDays(30)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

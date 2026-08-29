using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.StudentGroups;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class StudentGroupsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public StudentGroupsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededGroup_WithThreeMembers()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");

        var groups = await client.GetFromJsonAsync<List<StudentGroupDto>>("/api/studentgroups");

        Assert.NotNull(groups);
        var clubSciences = Assert.Single(groups!, g => g.Name == "Club Sciences");
        Assert.Equal(3, clubSciences.MemberCount);
    }

    [Fact]
    public async Task Director_CanCreateGroupAndBulkAddMembers()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var studentIds = students!.Take(2).Select(s => s.Id).ToList();

        var createResponse = await client.PostAsJsonAsync("/api/studentgroups", new CreateStudentGroupRequest(
            "Groupe Test", "Niveau", 10, currentYear.Id, null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<StudentGroupDto>();

        var addResponse = await client.PostAsJsonAsync($"/api/studentgroups/{created!.Id}/members", new AddGroupMembersRequest(studentIds));
        addResponse.EnsureSuccessStatusCode();
        var members = await addResponse.Content.ReadFromJsonAsync<List<StudentGroupMemberDto>>();

        Assert.Equal(2, members!.Count);

        var removeResponse = await client.DeleteAsync($"/api/studentgroups/{created.Id}/members/{studentIds[0]}");
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var remainingMembers = await client.GetFromJsonAsync<List<StudentGroupMemberDto>>($"/api/studentgroups/{created.Id}/members");
        Assert.Single(remainingMembers!);
    }

    [Fact]
    public async Task AddMembers_IsIdempotent_ForAlreadyExistingMember()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var studentId = students!.First().Id;

        var createResponse = await client.PostAsJsonAsync("/api/studentgroups", new CreateStudentGroupRequest(
            "Groupe Idempotent", "Niveau", null, currentYear.Id, null));
        var created = await createResponse.Content.ReadFromJsonAsync<StudentGroupDto>();

        await client.PostAsJsonAsync($"/api/studentgroups/{created!.Id}/members", new AddGroupMembersRequest(new List<Guid> { studentId }));
        var secondAddResponse = await client.PostAsJsonAsync($"/api/studentgroups/{created.Id}/members", new AddGroupMembersRequest(new List<Guid> { studentId }));

        secondAddResponse.EnsureSuccessStatusCode();
        var members = await secondAddResponse.Content.ReadFromJsonAsync<List<StudentGroupMemberDto>>();
        Assert.Single(members!);
    }

    [Fact]
    public async Task Teacher_CannotCreateGroup()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");

        var response = await client.PostAsJsonAsync("/api/studentgroups", new CreateStudentGroupRequest(
            "Interdit", "Niveau", null, years!.First().Id, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

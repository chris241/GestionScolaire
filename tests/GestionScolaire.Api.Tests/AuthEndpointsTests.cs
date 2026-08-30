using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Auth;
using GestionScolaire.Application.DTOs.Schools;
using GestionScolaire.Application.DTOs.Teachers;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class AuthEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        var client = _factory.CreateClient();

        var auth = await client.LoginAsync("directeur@ecole.mg");

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.Equal("directeur@ecole.mg", auth.User.Email);
        Assert.Equal("Director", auth.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("directeur@ecole.mg", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("inconnu@ecole.mg", "Password123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ThenLogin_Succeeds()
    {
        var client = _factory.CreateClient();
        var email = $"nouveau.parent.{Guid.NewGuid():N}@ecole.mg";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, "Password123!", "Nouveau", "Parent", "Parent"));

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var auth = await client.LoginAsync(email);
        Assert.Equal(email, auth.User.Email);
        Assert.Equal("Parent", auth.User.Role);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            "directeur@ecole.mg", "Password123!", "Doublon", "Directeur", "Director"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsNewToken()
    {
        var client = _factory.CreateClient();
        var initialAuth = await client.LoginAsync("directeur@ecole.mg");

        var response = await client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest(initialAuth.AccessToken, initialAuth.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshed = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.AccessToken));
    }

    [Fact]
    public async Task Login_AsDirector_ReturnsActiveSchoolAndAvailableSchools()
    {
        var client = _factory.CreateClient();

        var auth = await client.LoginAsync("directeur@ecole.mg");

        Assert.NotNull(auth.User.ActiveSchoolId);
        Assert.Equal("Lumière", auth.User.ActiveSchoolName);
        Assert.Equal(2, auth.User.AvailableSchools.Count);
        Assert.Contains(auth.User.AvailableSchools, s => s.Name == "Lumière");
        Assert.Contains(auth.User.AvailableSchools, s => s.Name == "Génie");
    }

    [Fact]
    public async Task Login_AsTeacherLinkedToOneSchool_HasNoOtherAvailableSchool()
    {
        var client = _factory.CreateClient();

        var auth = await client.LoginAsync("prof.math@ecole.mg");

        Assert.Equal("Lumière", auth.User.ActiveSchoolName);
        Assert.Single(auth.User.AvailableSchools);
    }

    [Fact]
    public async Task SwitchSchool_Director_ReissuesTokenWithNewActiveSchool_AndScopesTeacherListing()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var schools = await client.GetFromJsonAsync<List<SchoolDto>>("/api/schools");
        var genie = schools!.Single(s => s.Name == "Génie");
        var lumiere = schools!.Single(s => s.Name == "Lumière");

        var teachersInLumiere = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        Assert.Contains(teachersInLumiere!, t => t.Email == "prof.math@ecole.mg");
        Assert.Contains(teachersInLumiere!, t => t.Email == "prof.francais@ecole.mg");

        var switchResponse = await client.PostAsJsonAsync("/api/auth/switch-school", new SwitchSchoolRequest(genie.Id));
        switchResponse.EnsureSuccessStatusCode();
        var switched = await switchResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(genie.Id, switched!.User.ActiveSchoolId);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", switched.AccessToken);

        var teachersInGenie = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        Assert.DoesNotContain(teachersInGenie!, t => t.Email == "prof.math@ecole.mg");
        Assert.Contains(teachersInGenie!, t => t.Email == "prof.francais@ecole.mg");

        // Remet le directeur sur sa première école pour ne pas affecter les autres tests partageant la base.
        await client.PostAsJsonAsync("/api/auth/switch-school", new SwitchSchoolRequest(lumiere.Id));
    }

    [Fact]
    public async Task SwitchSchool_TeacherLinkedToTwoSchools_CanSwitchBetweenThem()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.francais@ecole.mg");

        var schools = await client.GetFromJsonAsync<List<SchoolDto>>("/api/schools");
        var genie = schools!.Single(s => s.Name == "Génie");
        var lumiere = schools!.Single(s => s.Name == "Lumière");

        var response = await client.PostAsJsonAsync("/api/auth/switch-school", new SwitchSchoolRequest(genie.Id));
        response.EnsureSuccessStatusCode();
        var switched = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(genie.Id, switched!.User.ActiveSchoolId);

        // Remet l'enseignant sur sa première école pour ne pas affecter les autres tests partageant la base.
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", switched.AccessToken);
        await client.PostAsJsonAsync("/api/auth/switch-school", new SwitchSchoolRequest(lumiere.Id));
    }

    [Fact]
    public async Task SwitchSchool_TeacherLinkedToOneSchool_CannotSwitchToOtherSchool()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var genie = (await directorClient.GetFromJsonAsync<List<SchoolDto>>("/api/schools"))!.Single(s => s.Name == "Génie");

        var response = await client.PostAsJsonAsync("/api/auth/switch-school", new SwitchSchoolRequest(genie.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SwitchSchool_Director_CannotSwitchToAnotherDirectorsSchool()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var otherEmail = $"autre.directeur.{Guid.NewGuid():N}@ecole.mg";
        var otherDirectorClient = _factory.CreateClient();
        await otherDirectorClient.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            otherEmail, AuthHelper.DemoPassword, "Autre", "Directeur", "Director"));
        var otherDirectorAuth = await otherDirectorClient.AsUserAsync(otherEmail);

        var createResponse = await otherDirectorAuth.PostAsJsonAsync("/api/schools", new CreateSchoolRequest(
            "École Rivale", null, "MGA", 20));
        var rivalSchool = await createResponse.Content.ReadFromJsonAsync<SchoolDto>();

        var response = await client.PostAsJsonAsync("/api/auth/switch-school", new SwitchSchoolRequest(rivalSchool!.Id));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CrossTenantIsolation_NewDirectorsSchool_NeverLeaksSeededSchoolData()
    {
        var email = $"isole.directeur.{Guid.NewGuid():N}@ecole.mg";
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            email, AuthHelper.DemoPassword, "Isole", "Directeur", "Director"));
        var authedClient = await client.AsUserAsync(email);

        await authedClient.PostAsJsonAsync("/api/schools", new CreateSchoolRequest("École Isolée", null, "MGA", 20));

        // Re-login pour que le token porte la nouvelle école (créée sans bascule automatique).
        var reloginAuth = await client.LoginAsync(email);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", reloginAuth.AccessToken);

        var teachers = await client.GetFromJsonAsync<List<TeacherDto>>("/api/teachers");
        Assert.Empty(teachers!);

        var mySchools = await client.GetFromJsonAsync<List<SchoolDto>>("/api/schools");
        Assert.Single(mySchools!);
        Assert.Equal("École Isolée", mySchools!.Single().Name);
    }
}

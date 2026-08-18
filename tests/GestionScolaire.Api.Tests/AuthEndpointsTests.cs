using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Auth;
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
}

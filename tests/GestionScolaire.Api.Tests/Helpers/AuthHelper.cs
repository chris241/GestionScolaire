using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionScolaire.Application.DTOs.Auth;

namespace GestionScolaire.Api.Tests.Helpers;

public static class AuthHelper
{
    public const string DemoPassword = "Password123!";

    public static async Task<AuthResponse> LoginAsync(this HttpClient client, string email, string password = DemoPassword)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return body ?? throw new InvalidOperationException("Réponse de connexion vide.");
    }

    public static async Task<HttpClient> AsUserAsync(this HttpClient client, string email, string password = DemoPassword)
    {
        var auth = await client.LoginAsync(email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}

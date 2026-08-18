using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace GestionScolaire.Api.Tests;

/// Démarre un vrai PostgreSQL éphémère (Testcontainers) et héberge l'API en mémoire.
/// L'environnement "Development" est conservé pour que Program.cs applique les migrations
/// et exécute le DbSeeder au démarrage, ce qui fournit des comptes/données de test connus.
public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("gestionscolaire_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Jwt:Secret"] = "integration_tests_super_secret_key_min_32_chars",
                ["Jwt:Issuer"] = "GestionScolaire.Api.Tests",
                ["Jwt:Audience"] = "GestionScolaire.Api.Tests",
                ["DISABLE_HTTPS_REDIRECT"] = "true",
            });
        });
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiTestCollection : ICollectionFixture<ApiWebApplicationFactory>
{
    public const string Name = "Api integration tests";
}

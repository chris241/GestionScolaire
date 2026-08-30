using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Courses;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class CoursesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public CoursesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Director_CanAddTopicWithContent_AndUpdateIt()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var course = (await client.GetFromJsonAsync<List<CourseDto>>("/api/courses"))!.First();

        var createResponse = await client.PostAsJsonAsync($"/api/courses/{course.Id}/topics", new CreateTopicRequest(
            "Fractions", "Chapitre sur les fractions", "## Objectifs\nComprendre le calcul de fractions.", 3));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<TopicDto>();
        Assert.Equal("## Objectifs\nComprendre le calcul de fractions.", created!.Content);

        var updateResponse = await client.PutAsJsonAsync($"/api/courses/topics/{created.Id}", new UpdateTopicRequest(
            "Fractions et décimaux", created.Description, "Contenu mis à jour.", 3));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<TopicDto>();
        Assert.Equal("Contenu mis à jour.", updated!.Content);
        Assert.Equal("Fractions et décimaux", updated.Name);

        var refreshed = (await client.GetFromJsonAsync<List<CourseDto>>("/api/courses"))!.Single(c => c.Id == course.Id);
        Assert.Contains(refreshed.Topics, t => t.Id == created.Id && t.Content == "Contenu mis à jour.");
    }

    [Fact]
    public async Task Teacher_CanAddAndUpdateTopics()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var course = (await client.GetFromJsonAsync<List<CourseDto>>("/api/courses"))!.First();

        var createResponse = await client.PostAsJsonAsync($"/api/courses/{course.Id}/topics", new CreateTopicRequest(
            "Chapitre enseignant", null, "Contenu ajouté par un enseignant.", 4));

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<TopicDto>();
        Assert.Equal("Contenu ajouté par un enseignant.", created!.Content);
    }

    [Fact]
    public async Task Parent_CannotAddTopic()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var course = (await client.GetFromJsonAsync<List<CourseDto>>("/api/courses"))!.First();

        var response = await client.PostAsJsonAsync($"/api/courses/{course.Id}/topics", new CreateTopicRequest(
            "Interdit", null, null, 1));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

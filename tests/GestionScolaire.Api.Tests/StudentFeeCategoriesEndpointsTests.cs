using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.FeeCategories;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class StudentFeeCategoriesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public StudentFeeCategoriesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetFeeCategories_ReflectsMandatoryAndExplicitSubscriptions_ForSeededStudents()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var tojo = students!.Single(s => s.FirstName == "Tojo");
        var fara = students!.Single(s => s.FirstName == "Fara");

        // Tojo est abonné à Cantine et Transport en plus de la Scolarité obligatoire (voir DbSeeder).
        var tojoCategories = await client.GetFromJsonAsync<List<StudentFeeCategoryDto>>($"/api/students/{tojo.Id}/fee-categories");
        Assert.All(tojoCategories!, c => Assert.True(c.IsSubscribed));

        // Fara n'a que la Scolarité (obligatoire) ; elle n'est pas abonnée à Cantine/Transport.
        var faraCategories = await client.GetFromJsonAsync<List<StudentFeeCategoryDto>>($"/api/students/{fara.Id}/fee-categories");
        Assert.True(faraCategories!.Single(c => c.FeeCategoryName == "Scolarité").IsSubscribed);
        Assert.False(faraCategories!.Single(c => c.FeeCategoryName == "Cantine").IsSubscribed);
        Assert.False(faraCategories!.Single(c => c.FeeCategoryName == "Transport").IsSubscribed);
    }

    [Fact]
    public async Task Director_CanSubscribeAndUnsubscribeStudent_ToOptionalCategory()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var fara = students!.Single(s => s.FirstName == "Fara");
        var categories = await client.GetFromJsonAsync<List<FeeCategoryDto>>("/api/feecategories");
        var cantine = categories!.Single(c => c.Name == "Cantine");

        var subscribeResponse = await client.PostAsync($"/api/students/{fara.Id}/fee-categories/{cantine.Id}", null);
        Assert.Equal(HttpStatusCode.NoContent, subscribeResponse.StatusCode);

        var afterSubscribe = await client.GetFromJsonAsync<List<StudentFeeCategoryDto>>($"/api/students/{fara.Id}/fee-categories");
        Assert.True(afterSubscribe!.Single(c => c.FeeCategoryName == "Cantine").IsSubscribed);

        var unsubscribeResponse = await client.DeleteAsync($"/api/students/{fara.Id}/fee-categories/{cantine.Id}");
        Assert.Equal(HttpStatusCode.NoContent, unsubscribeResponse.StatusCode);

        var afterUnsubscribe = await client.GetFromJsonAsync<List<StudentFeeCategoryDto>>($"/api/students/{fara.Id}/fee-categories");
        Assert.False(afterUnsubscribe!.Single(c => c.FeeCategoryName == "Cantine").IsSubscribed);
    }

    [Fact]
    public async Task Director_CannotSubscribeStudent_ToMandatoryCategory()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var fara = students!.Single(s => s.FirstName == "Fara");
        var categories = await client.GetFromJsonAsync<List<FeeCategoryDto>>("/api/feecategories");
        var scolarite = categories!.Single(c => c.IsMandatory);

        var response = await client.PostAsync($"/api/students/{fara.Id}/fee-categories/{scolarite.Id}", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotManageFeeCategorySubscriptions()
    {
        var teacherClient = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var students = await directorClient.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var fara = students!.Single(s => s.FirstName == "Fara");

        var response = await teacherClient.GetAsync($"/api/students/{fara.Id}/fee-categories");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

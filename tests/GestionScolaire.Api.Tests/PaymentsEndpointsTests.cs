using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Payments;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class PaymentsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public PaymentsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Director_SeesGlobalPaymentsList()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var response = await client.GetAsync("/api/payments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>();
        Assert.Equal(8, payments!.Count);
    }

    [Fact]
    public async Task Parent_CannotSeeGlobalPaymentsList()
    {
        var client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");

        var response = await client.GetAsync("/api/payments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotSeeGlobalPaymentsList()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.GetAsync("/api/payments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Parent_CanSeeOwnChildPayments()
    {
        var parentClient = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var ownChild = (await parentClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var response = await parentClient.GetAsync($"/api/payments/student/{ownChild.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payments = await response.Content.ReadFromJsonAsync<List<PaymentDto>>();
        Assert.Single(payments!);
    }

    [Fact]
    public async Task Parent_CannotSeeAnotherChildPayments()
    {
        var parent1Client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2Client = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var otherChild = (await parent2Client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var response = await parent1Client.GetAsync($"/api/payments/student/{otherChild.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

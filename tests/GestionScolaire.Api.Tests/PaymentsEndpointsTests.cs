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
        Assert.Equal(9, payments!.Count);
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
        Assert.Equal(2, payments!.Count);
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

    [Fact]
    public async Task Director_CanRecordPayment_WithoutInvoice()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var student = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First();

        var response = await client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest(
            student.Id, "Frais de cantine", 40000, "2025-2026", "Trimestre 1", "Espèces", null));

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.Equal("Paye", created!.Status);
        Assert.NotNull(created.PaidAt);
        Assert.Null(created.InvoiceId);
    }

    [Fact]
    public async Task Teacher_CannotRecordPayment()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");
        var student = (await client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.First();

        var response = await client.PostAsJsonAsync("/api/payments", new CreatePaymentRequest(
            student.Id, "Interdit", 1000, "2025-2026", "Trimestre 1", "Espèces", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

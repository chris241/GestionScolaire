using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Invoices;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class InvoicesEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public InvoicesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Director_SeesSeededInvoices_PaidAndPending()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var invoices = await client.GetFromJsonAsync<List<InvoiceDto>>("/api/invoices");

        Assert.NotNull(invoices);
        Assert.True(invoices!.Count >= 2);
        Assert.Contains(invoices, i => i.Status == "Paye");
        Assert.Contains(invoices, i => i.Status == "EnAttente");
    }

    [Fact]
    public async Task Parent_CanSeeOwnChildInvoices_ButNotOtherChild()
    {
        var parent1 = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2 = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");

        var ownChild = (await parent1.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();
        var otherChild = (await parent2.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var ownResponse = await parent1.GetAsync($"/api/invoices/student/{ownChild.Id}");
        ownResponse.EnsureSuccessStatusCode();
        var ownInvoices = await ownResponse.Content.ReadFromJsonAsync<List<InvoiceDto>>();
        Assert.Contains(ownInvoices!, i => i.Status == "Paye");

        var otherResponse = await parent1.GetAsync($"/api/invoices/student/{otherChild.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotListAllInvoices()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.GetAsync("/api/invoices");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOverdueReport_ListsSeededPendingInvoice_NotThePaidOne()
    {
        // La facture "En attente" du seed a une échéance en 2025, donc déjà dépassée à l'exécution des tests.
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var allInvoices = await client.GetFromJsonAsync<List<InvoiceDto>>("/api/invoices");
        // Numéros de facture déterministes plutôt que Single(Status==...) : d'autres tests peuvent créer
        // des factures supplémentaires payées/en attente dans la même base partagée.
        var pendingInvoice = allInvoices!.Single(i => i.InvoiceNumber == "FAC-2025T1-002");
        var paidInvoice = allInvoices!.Single(i => i.InvoiceNumber == "FAC-2025T1-001");

        var overdue = await client.GetFromJsonAsync<List<OverdueInvoiceDto>>("/api/invoices/reports/overdue");

        Assert.Contains(overdue!, i => i.Id == pendingInvoice.Id);
        Assert.DoesNotContain(overdue!, i => i.Id == paidInvoice.Id);
        Assert.All(overdue!, i => Assert.True(i.DaysLate > 0));
    }

    [Fact]
    public async Task Teacher_CannotAccessOverdueReport()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.GetAsync("/api/invoices/reports/overdue");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

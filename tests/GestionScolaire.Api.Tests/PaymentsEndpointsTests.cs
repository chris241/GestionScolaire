using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.FeeCategories;
using GestionScolaire.Application.DTOs.FeeStructures;
using GestionScolaire.Application.DTOs.Invoices;
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

    [Fact]
    public async Task Parent_CanDeclarePayment_ForOwnChild_AndDirectorCanValidateIt()
    {
        // Déclaration ad hoc (sans facture) pour ne pas dépendre/altérer l'état des factures seedées.
        // parent4 (et non parent1/parent2, déjà utilisés par des assertions à compte fixe ailleurs dans
        // ce fichier et dans InvoicesEndpointsTests) pour ne pas perturber ces tests.
        var parentClient = await _factory.CreateClient().AsUserAsync("parent4@ecole.mg");
        var child = (await parentClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var declareResponse = await parentClient.PostAsJsonAsync("/api/payments/declare", new DeclarePaymentRequest(
            child.Id, "Cantine — Octobre", 40000, "2025-2026", "Trimestre 1", "Mobile Money", null));

        declareResponse.EnsureSuccessStatusCode();
        var declared = await declareResponse.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.Equal("EnValidation", declared!.Status);
        Assert.Null(declared.PaidAt);

        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var pending = await directorClient.GetFromJsonAsync<List<PaymentDto>>("/api/payments/pending");
        Assert.Contains(pending!, p => p.Id == declared.Id);

        var validateResponse = await directorClient.PutAsync($"/api/payments/{declared.Id}/validate", null);
        validateResponse.EnsureSuccessStatusCode();
        var validated = await validateResponse.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.Equal("Paye", validated!.Status);
        Assert.NotNull(validated.PaidAt);

        // Retiré de la file d'attente une fois validé.
        var pendingAfter = await directorClient.GetFromJsonAsync<List<PaymentDto>>("/api/payments/pending");
        Assert.DoesNotContain(pendingAfter!, p => p.Id == declared.Id);
    }

    [Fact]
    public async Task Director_ValidatingDeclaredPayment_AlsoMarksLinkedInvoicePaid()
    {
        // Construit sa propre structure/échéance/facture isolée plutôt que de toucher la facture "En
        // attente" du seed, partagée avec d'autres tests.
        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var years = await directorClient.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);
        var terms = await directorClient.GetFromJsonAsync<List<GestionScolaire.Application.DTOs.AcademicTerms.AcademicTermDto>>(
            $"/api/academicterms?academicYearId={currentYear.Id}");
        var categories = await directorClient.GetFromJsonAsync<List<FeeCategoryDto>>("/api/feecategories");

        var structureResponse = await directorClient.PostAsJsonAsync("/api/feestructures", new CreateFeeStructureRequest(
            "Frais Validation Test", currentYear.Id, null));
        var structure = await structureResponse.Content.ReadFromJsonAsync<FeeStructureDto>();
        await directorClient.PostAsJsonAsync($"/api/feestructures/{structure!.Id}/items",
            new CreateFeeStructureItemRequest(categories!.First().Id, 40000));
        var scheduleResponse = await directorClient.PostAsJsonAsync($"/api/feestructures/{structure.Id}/schedules",
            new CreateFeeScheduleRequest(terms!.First().Id, DateTime.UtcNow.AddDays(10)));
        var schedule = await scheduleResponse.Content.ReadFromJsonAsync<FeeScheduleDto>();
        await directorClient.PostAsync($"/api/feestructures/schedules/{schedule!.Id}/generate-invoices", null);

        var parentClient = await _factory.CreateClient().AsUserAsync("parent5@ecole.mg");
        var child = (await parentClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();
        var invoices = await parentClient.GetFromJsonAsync<List<InvoiceDto>>($"/api/invoices/student/{child.Id}");
        var newInvoice = invoices!.Single(i => i.FeeScheduleId == schedule.Id);

        var declareResponse = await parentClient.PostAsJsonAsync("/api/payments/declare", new DeclarePaymentRequest(
            child.Id, "Frais Validation Test", newInvoice.TotalAmount, "2025-2026", "Trimestre 1", "Mobile Money", newInvoice.Id));
        var declared = await declareResponse.Content.ReadFromJsonAsync<PaymentDto>();

        await directorClient.PutAsync($"/api/payments/{declared!.Id}/validate", null);

        var invoicesAfter = await directorClient.GetFromJsonAsync<List<InvoiceDto>>($"/api/invoices/student/{child.Id}");
        Assert.Equal("Paye", invoicesAfter!.Single(i => i.Id == newInvoice.Id).Status);
    }

    [Fact]
    public async Task Director_CanRejectDeclaredPayment()
    {
        var parentClient = await _factory.CreateClient().AsUserAsync("parent3@ecole.mg");
        var child = (await parentClient.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var declareResponse = await parentClient.PostAsJsonAsync("/api/payments/declare", new DeclarePaymentRequest(
            child.Id, "Cantine — Septembre", 40000, "2025-2026", "Trimestre 1", "Mobile Money", null));
        declareResponse.EnsureSuccessStatusCode();
        var declared = await declareResponse.Content.ReadFromJsonAsync<PaymentDto>();

        var directorClient = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var rejectResponse = await directorClient.PutAsJsonAsync($"/api/payments/{declared!.Id}/reject",
            new RejectPaymentRequest("Référence Mobile Money introuvable."));

        rejectResponse.EnsureSuccessStatusCode();
        var rejected = await rejectResponse.Content.ReadFromJsonAsync<PaymentDto>();
        Assert.Equal("Annule", rejected!.Status);
        Assert.Equal("Référence Mobile Money introuvable.", rejected.DecisionNotes);
        Assert.Null(rejected.PaidAt);
    }

    [Fact]
    public async Task Parent_CannotDeclarePayment_ForAnotherParentsChild()
    {
        var parent1Client = await _factory.CreateClient().AsUserAsync("parent1@ecole.mg");
        var parent2Client = await _factory.CreateClient().AsUserAsync("parent2@ecole.mg");
        var otherChild = (await parent2Client.GetFromJsonAsync<List<StudentDto>>("/api/students"))!.Single();

        var response = await parent1Client.PostAsJsonAsync("/api/payments/declare", new DeclarePaymentRequest(
            otherChild.Id, "Tentative frauduleuse", 250000, "2025-2026", "Trimestre 1", "Espèces", null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Director_CannotValidateAlreadyDecidedPayment()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");
        var alreadyPaid = (await client.GetFromJsonAsync<List<PaymentDto>>("/api/payments"))!.First(p => p.Status == "Paye");

        var response = await client.PutAsync($"/api/payments/{alreadyPaid.Id}/validate", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_FiltersByAcademicYear()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var payments = await client.GetFromJsonAsync<List<PaymentDto>>("/api/payments?academicYear=2025-2026");
        Assert.NotEmpty(payments!);

        var noneForOtherYear = await client.GetFromJsonAsync<List<PaymentDto>>("/api/payments?academicYear=2099-2100");
        Assert.Empty(noneForOtherYear!);
    }
}

using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.AcademicYears;
using GestionScolaire.Application.DTOs.FeeCategories;
using GestionScolaire.Application.DTOs.FeeStructures;
using Xunit;

namespace GestionScolaire.Api.Tests;

[Collection(ApiTestCollection.Name)]
public class FeeStructuresEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public FeeStructuresEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAll_ReturnsSeededStructure_WithThreeItemsAndOneSchedule()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var structures = await client.GetFromJsonAsync<List<FeeStructureDto>>("/api/feestructures");

        Assert.NotNull(structures);
        var seeded = Assert.Single(structures!, s => s.Name == "Frais standard 2025-2026");
        Assert.Equal(3, seeded.Items.Count);
        Assert.Equal(250000, seeded.TotalAmount);
        Assert.Single(seeded.Schedules);
    }

    [Fact]
    public async Task Director_GenerateInvoices_OnSeededSchedule_IsIdempotent()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var structures = await client.GetFromJsonAsync<List<FeeStructureDto>>("/api/feestructures");
        var scheduleId = structures!.Single(s => s.Name == "Frais standard 2025-2026").Schedules.Single().Id;

        var firstResponse = await client.PostAsync($"/api/feestructures/schedules/{scheduleId}/generate-invoices", null);
        firstResponse.EnsureSuccessStatusCode();
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<GenerateInvoicesResult>();

        Assert.Equal(6, firstResult!.Created);
        Assert.Equal(2, firstResult.AlreadyExisted);

        var secondResponse = await client.PostAsync($"/api/feestructures/schedules/{scheduleId}/generate-invoices", null);
        secondResponse.EnsureSuccessStatusCode();
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<GenerateInvoicesResult>();

        Assert.Equal(0, secondResult!.Created);
        Assert.Equal(8, secondResult.AlreadyExisted);
    }

    [Fact]
    public async Task Director_CanCreateStructure_WithItemsAndSchedule()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);
        var terms = await client.GetFromJsonAsync<List<GestionScolaire.Application.DTOs.AcademicTerms.AcademicTermDto>>(
            $"/api/academicterms?academicYearId={currentYear.Id}");
        var categories = await client.GetFromJsonAsync<List<FeeCategoryDto>>("/api/feecategories");

        var createResponse = await client.PostAsJsonAsync("/api/feestructures", new CreateFeeStructureRequest(
            "Frais Test", currentYear.Id, null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<FeeStructureDto>();

        var itemResponse = await client.PostAsJsonAsync($"/api/feestructures/{created!.Id}/items",
            new CreateFeeStructureItemRequest(categories!.First().Id, 15000));
        itemResponse.EnsureSuccessStatusCode();

        var scheduleResponse = await client.PostAsJsonAsync($"/api/feestructures/{created.Id}/schedules",
            new CreateFeeScheduleRequest(terms!.First().Id, DateTime.UtcNow.AddDays(30)));
        scheduleResponse.EnsureSuccessStatusCode();

        var structures = await client.GetFromJsonAsync<List<FeeStructureDto>>("/api/feestructures");
        var refreshed = structures!.Single(s => s.Id == created.Id);
        Assert.Single(refreshed.Items);
        Assert.Single(refreshed.Schedules);
        Assert.Equal(15000, refreshed.TotalAmount);
    }

    [Fact]
    public async Task Director_CanGenerateMonthlySchedules_ForWholeTerm_Idempotently()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var years = await client.GetFromJsonAsync<List<AcademicYearDto>>("/api/academicyears");
        var currentYear = years!.Single(y => y.IsCurrent);
        var terms = await client.GetFromJsonAsync<List<GestionScolaire.Application.DTOs.AcademicTerms.AcademicTermDto>>(
            $"/api/academicterms?academicYearId={currentYear.Id}");
        // Trimestre 1 du seed va du 1er septembre au 19 décembre : 4 mois calendaires (sept./oct./nov./déc.).
        var term = terms!.Single(t => t.Name == "Trimestre 1");
        var categories = await client.GetFromJsonAsync<List<FeeCategoryDto>>("/api/feecategories");

        var createResponse = await client.PostAsJsonAsync("/api/feestructures", new CreateFeeStructureRequest(
            "Frais Mensuels Test", currentYear.Id, null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<FeeStructureDto>();

        await client.PostAsJsonAsync($"/api/feestructures/{created!.Id}/items",
            new CreateFeeStructureItemRequest(categories!.First().Id, 20000));

        var firstResponse = await client.PostAsJsonAsync($"/api/feestructures/{created.Id}/schedules/monthly",
            new GenerateMonthlySchedulesRequest(term.Id, 5));
        firstResponse.EnsureSuccessStatusCode();
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<GenerateMonthlySchedulesResult>();

        Assert.Equal(4, firstResult!.SchedulesCreated);
        Assert.Equal(0, firstResult.SchedulesAlreadyExisted);
        Assert.Equal(4 * 8, firstResult.InvoicesCreated);

        var structures = await client.GetFromJsonAsync<List<FeeStructureDto>>("/api/feestructures");
        Assert.Equal(4, structures!.Single(s => s.Id == created.Id).Schedules.Count);

        // Idempotent au niveau du mois : relancer ne recrée aucune échéance.
        var secondResponse = await client.PostAsJsonAsync($"/api/feestructures/{created.Id}/schedules/monthly",
            new GenerateMonthlySchedulesRequest(term.Id, 5));
        secondResponse.EnsureSuccessStatusCode();
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<GenerateMonthlySchedulesResult>();

        Assert.Equal(0, secondResult!.SchedulesCreated);
        Assert.Equal(4, secondResult.SchedulesAlreadyExisted);
    }

    [Fact]
    public async Task Teacher_CannotAccessFeeStructures()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.GetAsync("/api/feestructures");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

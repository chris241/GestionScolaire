using System.Net;
using System.Net.Http.Json;
using GestionScolaire.Api.Tests.Helpers;
using GestionScolaire.Application.DTOs.Invoices;
using GestionScolaire.Application.DTOs.Students;
using Xunit;

namespace GestionScolaire.Api.Tests;

/// Ces tests évitent les assertions sur des montants exacts : d'autres tests de la suite partagée
/// (génération de factures, enregistrement de paiements) peuvent augmenter les totaux entre-temps.
/// Seules des bornes minimales garanties par le seed initial sont vérifiées.
[Collection(ApiTestCollection.Name)]
public class FeeCollectionReportsEndpointsTests
{
    private readonly ApiWebApplicationFactory _factory;

    public FeeCollectionReportsEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Director_GetStudentCollectionReport_ReturnsAllActiveStudents_WithKnownPaidSeed()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var report = await client.GetFromJsonAsync<List<StudentFeeCollectionDto>>("/api/invoices/reports/student-collection");

        Assert.NotNull(report);
        Assert.Equal(students!.Count, report!.Count);

        var tojo = report.Single(r => r.StudentFullName == "Tojo Randria");
        Assert.True(tojo.InvoicedAmount >= 200000);
        Assert.True(tojo.PaidAmount >= 200000);
    }

    [Fact]
    public async Task Director_StudentCollectionReport_FiltersToClassId()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var classId = students!.First().ClassId;
        var expectedCount = students!.Count(s => s.ClassId == classId);

        var report = await client.GetFromJsonAsync<List<StudentFeeCollectionDto>>(
            $"/api/invoices/reports/student-collection?classId={classId}");

        Assert.NotNull(report);
        Assert.Equal(expectedCount, report!.Count);
        Assert.All(report, r => Assert.Contains(students!, s => s.Id == r.StudentId && s.ClassId == classId));
    }

    [Fact]
    public async Task Director_GetProgramCollectionReport_ReturnsSeededProgram_WithAllEnrolledStudents()
    {
        var client = await _factory.CreateClient().AsUserAsync("directeur@ecole.mg");

        var students = await client.GetFromJsonAsync<List<StudentDto>>("/api/students");
        var report = await client.GetFromJsonAsync<List<ProgramFeeCollectionDto>>("/api/invoices/reports/program-collection");

        Assert.NotNull(report);
        var collegeGeneral = Assert.Single(report!, r => r.ProgramName == "Collège Général");
        Assert.Equal(students!.Count, collegeGeneral.StudentCount);
        Assert.True(collegeGeneral.InvoicedAmount >= 400000);
        Assert.True(collegeGeneral.PaidAmount >= 200000);
    }

    [Fact]
    public async Task Teacher_CannotAccessStudentCollectionReport()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.GetAsync("/api/invoices/reports/student-collection");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotAccessProgramCollectionReport()
    {
        var client = await _factory.CreateClient().AsUserAsync("prof.math@ecole.mg");

        var response = await client.GetAsync("/api/invoices/reports/program-collection");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

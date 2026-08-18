using GestionScolaire.Application.Common.Interfaces;
using GestionScolaire.Application.DTOs.Grades;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GestionScolaire.Infrastructure.Services;

public class BulletinPdfService : IBulletinPdfService
{
    private static readonly string Primary = "#3B82F6";
    private static readonly string Slate = "#1E293B";
    private static readonly string SlateLight = "#64748B";
    private static readonly string BgLight = "#F8FAFC";
    private static readonly string Border = "#E2E8F0";

    public BulletinPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateBulletin(BulletinDto bulletin)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(Slate));

                page.Header().Element(c => ComposeHeader(c, bulletin));
                page.Content().Element(c => ComposeContent(c, bulletin));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, BulletinDto bulletin)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(bulletin.SchoolName).FontSize(16).Bold().FontColor(Slate);
                    col.Item().Text("Bulletin Scolaire").FontSize(11).FontColor(SlateLight);
                });

                row.ConstantItem(160).Background(BgLight).Padding(10).Column(col =>
                {
                    col.Item().Text($"Année : {bulletin.AcademicYear}").FontSize(9);
                    col.Item().Text($"Période : {bulletin.Term}").FontSize(9);
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Border);
        });
    }

    private void ComposeContent(IContainer container, BulletinDto bulletin)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Item().Background(BgLight).Padding(12).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Élève").FontSize(8).FontColor(SlateLight);
                    col.Item().Text(bulletin.StudentFullName).FontSize(12).Bold();
                });
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Matricule").FontSize(8).FontColor(SlateLight);
                    col.Item().Text(bulletin.EnrollmentNumber).FontSize(12).Bold();
                });
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Classe").FontSize(8).FontColor(SlateLight);
                    col.Item().Text(bulletin.ClassName).FontSize(12).Bold();
                });
            });

            column.Item().PaddingTop(20).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(3);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Matière");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Coeff.");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Moyenne");
                    header.Cell().Element(HeaderCellStyle).Text("Appréciation");
                });

                foreach (var subject in bulletin.Subjects)
                {
                    table.Cell().Element(BodyCellStyle).Text(subject.SubjectName);
                    table.Cell().Element(BodyCellStyle).AlignRight().Text(subject.Coefficient.ToString("0.#"));
                    table.Cell().Element(BodyCellStyle).AlignRight().Text($"{subject.Average:0.00}/20").Bold();
                    table.Cell().Element(BodyCellStyle).Text(subject.Appreciation ?? "-");
                }

                static IContainer HeaderCellStyle(IContainer c) =>
                    c.Background(Slate).Padding(6).DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));

                static IContainer BodyCellStyle(IContainer c) =>
                    c.BorderBottom(1).BorderColor(Border).Padding(6);
            });

            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Background(BgLight).Padding(12).Column(col =>
                {
                    col.Item().Text("Moyenne Générale").FontSize(9).FontColor(SlateLight);
                    col.Item().Text($"{bulletin.GeneralAverage:0.00}/20").FontSize(18).Bold().FontColor(Primary);
                });

                row.RelativeItem().Background(BgLight).Padding(12).Column(col =>
                {
                    col.Item().Text("Rang").FontSize(9).FontColor(SlateLight);
                    col.Item().Text($"{bulletin.ClassRank} / {bulletin.ClassSize}").FontSize(18).Bold();
                });

                row.RelativeItem().Background(BgLight).Padding(12).Column(col =>
                {
                    col.Item().Text("Mention").FontSize(9).FontColor(SlateLight);
                    col.Item().Text(bulletin.Mention).FontSize(18).Bold();
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Généré le ").FontSize(8).FontColor(SlateLight);
            text.Span(DateTime.Now.ToString("dd/MM/yyyy à HH:mm")).FontSize(8).FontColor(SlateLight);
        });
    }
}

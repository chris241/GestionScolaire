using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionCampaigns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdmissionCampaignId",
                table: "StudentApplicants",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdmissionCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionCampaigns_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionCampaignQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdmissionCampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quota = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionCampaignQuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdmissionCampaignQuotas_AcademicPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "AcademicPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionCampaignQuotas_AdmissionCampaigns_AdmissionCampaig~",
                        column: x => x.AdmissionCampaignId,
                        principalTable: "AdmissionCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentApplicants_AdmissionCampaignId",
                table: "StudentApplicants",
                column: "AdmissionCampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionCampaignQuotas_AdmissionCampaignId_ProgramId",
                table: "AdmissionCampaignQuotas",
                columns: new[] { "AdmissionCampaignId", "ProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionCampaignQuotas_ProgramId",
                table: "AdmissionCampaignQuotas",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionCampaigns_AcademicYearId",
                table: "AdmissionCampaigns",
                column: "AcademicYearId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentApplicants_AdmissionCampaigns_AdmissionCampaignId",
                table: "StudentApplicants",
                column: "AdmissionCampaignId",
                principalTable: "AdmissionCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentApplicants_AdmissionCampaigns_AdmissionCampaignId",
                table: "StudentApplicants");

            migrationBuilder.DropTable(
                name: "AdmissionCampaignQuotas");

            migrationBuilder.DropTable(
                name: "AdmissionCampaigns");

            migrationBuilder.DropIndex(
                name: "IX_StudentApplicants_AdmissionCampaignId",
                table: "StudentApplicants");

            migrationBuilder.DropColumn(
                name: "AdmissionCampaignId",
                table: "StudentApplicants");
        }
    }
}

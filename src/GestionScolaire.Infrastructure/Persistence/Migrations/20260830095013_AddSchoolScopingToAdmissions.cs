using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolScopingToAdmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "StudentApplicants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "AdmissionCampaigns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StudentApplicants_SchoolId",
                table: "StudentApplicants",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionCampaigns_SchoolId",
                table: "AdmissionCampaigns",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionCampaigns_Schools_SchoolId",
                table: "AdmissionCampaigns",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentApplicants_Schools_SchoolId",
                table: "StudentApplicants",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionCampaigns_Schools_SchoolId",
                table: "AdmissionCampaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentApplicants_Schools_SchoolId",
                table: "StudentApplicants");

            migrationBuilder.DropIndex(
                name: "IX_StudentApplicants_SchoolId",
                table: "StudentApplicants");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionCampaigns_SchoolId",
                table: "AdmissionCampaigns");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "StudentApplicants");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AdmissionCampaigns");
        }
    }
}

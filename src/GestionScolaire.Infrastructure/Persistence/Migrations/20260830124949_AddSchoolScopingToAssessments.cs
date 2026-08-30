using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolScopingToAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "GradingScales",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "AssessmentPlans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "AssessmentGroups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_GradingScales_SchoolId",
                table: "GradingScales",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPlans_SchoolId",
                table: "AssessmentPlans",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGroups_SchoolId",
                table: "AssessmentGroups",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentGroups_Schools_SchoolId",
                table: "AssessmentGroups",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssessmentPlans_Schools_SchoolId",
                table: "AssessmentPlans",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GradingScales_Schools_SchoolId",
                table: "GradingScales",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentGroups_Schools_SchoolId",
                table: "AssessmentGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_AssessmentPlans_Schools_SchoolId",
                table: "AssessmentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_GradingScales_Schools_SchoolId",
                table: "GradingScales");

            migrationBuilder.DropIndex(
                name: "IX_GradingScales_SchoolId",
                table: "GradingScales");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentPlans_SchoolId",
                table: "AssessmentPlans");

            migrationBuilder.DropIndex(
                name: "IX_AssessmentGroups_SchoolId",
                table: "AssessmentGroups");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "GradingScales");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AssessmentPlans");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AssessmentGroups");
        }
    }
}

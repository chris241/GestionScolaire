using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssessmentPlanId",
                table: "Grades",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssessmentGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Weightage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentGroups_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalTable: "AcademicTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradingScales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingScales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicTermId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradingScaleId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentPlans_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalTable: "AcademicTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentPlans_AssessmentGroups_AssessmentGroupId",
                        column: x => x.AssessmentGroupId,
                        principalTable: "AssessmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentPlans_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentPlans_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentPlans_GradingScales_GradingScaleId",
                        column: x => x.GradingScaleId,
                        principalTable: "GradingScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GradingScaleIntervals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GradingScaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Grade = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MinScore = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingScaleIntervals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradingScaleIntervals_GradingScales_GradingScaleId",
                        column: x => x.GradingScaleId,
                        principalTable: "GradingScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    AssessmentPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentCriteria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentCriteria_AssessmentPlans_AssessmentPlanId",
                        column: x => x.AssessmentPlanId,
                        principalTable: "AssessmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Grades_AssessmentPlanId",
                table: "Grades",
                column: "AssessmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCriteria_AssessmentPlanId",
                table: "AssessmentCriteria",
                column: "AssessmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentGroups_AcademicTermId_Name",
                table: "AssessmentGroups",
                columns: new[] { "AcademicTermId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPlans_AcademicTermId",
                table: "AssessmentPlans",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPlans_AssessmentGroupId",
                table: "AssessmentPlans",
                column: "AssessmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPlans_ClassId",
                table: "AssessmentPlans",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPlans_CourseId",
                table: "AssessmentPlans",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentPlans_GradingScaleId",
                table: "AssessmentPlans",
                column: "GradingScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_GradingScaleIntervals_GradingScaleId",
                table: "GradingScaleIntervals",
                column: "GradingScaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_AssessmentPlans_AssessmentPlanId",
                table: "Grades",
                column: "AssessmentPlanId",
                principalTable: "AssessmentPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_AssessmentPlans_AssessmentPlanId",
                table: "Grades");

            migrationBuilder.DropTable(
                name: "AssessmentCriteria");

            migrationBuilder.DropTable(
                name: "GradingScaleIntervals");

            migrationBuilder.DropTable(
                name: "AssessmentPlans");

            migrationBuilder.DropTable(
                name: "AssessmentGroups");

            migrationBuilder.DropTable(
                name: "GradingScales");

            migrationBuilder.DropIndex(
                name: "IX_Grades_AssessmentPlanId",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "AssessmentPlanId",
                table: "Grades");
        }
    }
}

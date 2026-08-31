using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentFeeCategorySubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Invoices_StudentId_FeeScheduleId",
                table: "Invoices");

            migrationBuilder.AddColumn<Guid>(
                name: "FeeStructureItemId",
                table: "Invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "IsMandatory",
                table: "FeeCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StudentFeeCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeeCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentFeeCategories_FeeCategories_FeeCategoryId",
                        column: x => x.FeeCategoryId,
                        principalTable: "FeeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentFeeCategories_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_FeeStructureItemId",
                table: "Invoices",
                column: "FeeStructureItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_StudentId_FeeScheduleId_FeeStructureItemId",
                table: "Invoices",
                columns: new[] { "StudentId", "FeeScheduleId", "FeeStructureItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeCategories_FeeCategoryId",
                table: "StudentFeeCategories",
                column: "FeeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeCategories_StudentId_FeeCategoryId",
                table: "StudentFeeCategories",
                columns: new[] { "StudentId", "FeeCategoryId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_FeeStructureItems_FeeStructureItemId",
                table: "Invoices",
                column: "FeeStructureItemId",
                principalTable: "FeeStructureItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_FeeStructureItems_FeeStructureItemId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "StudentFeeCategories");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_FeeStructureItemId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_StudentId_FeeScheduleId_FeeStructureItemId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "FeeStructureItemId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsMandatory",
                table: "FeeCategories");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_StudentId_FeeScheduleId",
                table: "Invoices",
                columns: new[] { "StudentId", "FeeScheduleId" },
                unique: true);
        }
    }
}

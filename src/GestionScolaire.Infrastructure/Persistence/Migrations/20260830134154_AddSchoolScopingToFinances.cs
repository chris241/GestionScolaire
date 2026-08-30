using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolScopingToFinances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeeCategories_Name",
                table: "FeeCategories");

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Invoices",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "FeeCategories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SchoolId",
                table: "Payments",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_SchoolId",
                table: "Invoices",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeCategories_SchoolId_Name",
                table: "FeeCategories",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeCategories_Schools_SchoolId",
                table: "FeeCategories",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Schools_SchoolId",
                table: "Invoices",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Schools_SchoolId",
                table: "Payments",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeeCategories_Schools_SchoolId",
                table: "FeeCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Schools_SchoolId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Schools_SchoolId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SchoolId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_SchoolId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_FeeCategories_SchoolId_Name",
                table: "FeeCategories");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "FeeCategories");

            migrationBuilder.CreateIndex(
                name: "IX_FeeCategories_Name",
                table: "FeeCategories",
                column: "Name",
                unique: true);
        }
    }
}

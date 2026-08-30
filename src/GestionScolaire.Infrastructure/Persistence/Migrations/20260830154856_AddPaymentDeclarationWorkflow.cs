using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentDeclarationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DecisionDate",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionNotes",
                table: "Payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeclaredByUserId",
                table: "Payments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecisionDate",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DecisionNotes",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeclaredByUserId",
                table: "Payments");
        }
    }
}

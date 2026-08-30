using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolScopingToGuardiansAndLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "TeacherLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "StudentLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Guardians",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLogs_SchoolId",
                table: "TeacherLogs",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLogs_SchoolId",
                table: "StudentLogs",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_SchoolId",
                table: "Guardians",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Guardians_Schools_SchoolId",
                table: "Guardians",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentLogs_Schools_SchoolId",
                table: "StudentLogs",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherLogs_Schools_SchoolId",
                table: "TeacherLogs",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guardians_Schools_SchoolId",
                table: "Guardians");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentLogs_Schools_SchoolId",
                table: "StudentLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherLogs_Schools_SchoolId",
                table: "TeacherLogs");

            migrationBuilder.DropIndex(
                name: "IX_TeacherLogs_SchoolId",
                table: "TeacherLogs");

            migrationBuilder.DropIndex(
                name: "IX_StudentLogs_SchoolId",
                table: "StudentLogs");

            migrationBuilder.DropIndex(
                name: "IX_Guardians_SchoolId",
                table: "Guardians");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "TeacherLogs");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "StudentLogs");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Guardians");
        }
    }
}

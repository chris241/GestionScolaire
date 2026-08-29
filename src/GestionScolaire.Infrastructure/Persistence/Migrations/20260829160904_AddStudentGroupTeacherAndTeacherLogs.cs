using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentGroupTeacherAndTeacherLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeacherId",
                table: "StudentGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeacherLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    LogDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LogType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherLogs_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_TeacherId",
                table: "StudentGroups",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLogs_TeacherId_LogDate",
                table: "TeacherLogs",
                columns: new[] { "TeacherId", "LogDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroups_Teachers_TeacherId",
                table: "StudentGroups",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroups_Teachers_TeacherId",
                table: "StudentGroups");

            migrationBuilder.DropTable(
                name: "TeacherLogs");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_TeacherId",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "StudentGroups");
        }
    }
}

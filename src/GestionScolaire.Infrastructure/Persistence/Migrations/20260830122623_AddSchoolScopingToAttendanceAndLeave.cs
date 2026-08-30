using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolScopingToAttendanceAndLeave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "StudentLeaveApplications",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StudentLeaveApplications_SchoolId",
                table: "StudentLeaveApplications",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentLeaveApplications_Schools_SchoolId",
                table: "StudentLeaveApplications",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentLeaveApplications_Schools_SchoolId",
                table: "StudentLeaveApplications");

            migrationBuilder.DropIndex(
                name: "IX_StudentLeaveApplications_SchoolId",
                table: "StudentLeaveApplications");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "StudentLeaveApplications");
        }
    }
}

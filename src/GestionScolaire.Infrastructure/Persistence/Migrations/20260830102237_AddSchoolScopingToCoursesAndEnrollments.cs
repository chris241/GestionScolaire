using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolScopingToCoursesAndEnrollments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_Name",
                table: "Subjects");

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Subjects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "ProgramEnrollments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "CourseSchedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Courses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "CourseEnrollments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SchoolId_Name",
                table: "Subjects",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramEnrollments_SchoolId",
                table: "ProgramEnrollments",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseSchedules_SchoolId",
                table: "CourseSchedules",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SchoolId",
                table: "Courses",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_SchoolId",
                table: "CourseEnrollments",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_Schools_SchoolId",
                table: "CourseEnrollments",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Schools_SchoolId",
                table: "Courses",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseSchedules_Schools_SchoolId",
                table: "CourseSchedules",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramEnrollments_Schools_SchoolId",
                table: "ProgramEnrollments",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Schools_SchoolId",
                table: "Subjects",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_Schools_SchoolId",
                table: "CourseEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Schools_SchoolId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_CourseSchedules_Schools_SchoolId",
                table: "CourseSchedules");

            migrationBuilder.DropForeignKey(
                name: "FK_ProgramEnrollments_Schools_SchoolId",
                table: "ProgramEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Schools_SchoolId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_SchoolId_Name",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_ProgramEnrollments_SchoolId",
                table: "ProgramEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseSchedules_SchoolId",
                table: "CourseSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Courses_SchoolId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_SchoolId",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "ProgramEnrollments");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "CourseSchedules");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "CourseEnrollments");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Name",
                table: "Subjects",
                column: "Name",
                unique: true);
        }
    }
}

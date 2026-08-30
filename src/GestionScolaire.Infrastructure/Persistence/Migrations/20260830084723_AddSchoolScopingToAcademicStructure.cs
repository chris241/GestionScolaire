using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolScopingToAcademicStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentCategories_Name",
                table: "StudentCategories");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_Name",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_AcademicYears_Name",
                table: "AcademicYears");

            migrationBuilder.DropIndex(
                name: "IX_AcademicPrograms_Code",
                table: "AcademicPrograms");

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "StudentGroups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "StudentCategories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "StudentBatches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Rooms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "AcademicYears",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "AcademicTerms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "AcademicPrograms",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_SchoolId",
                table: "StudentGroups",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCategories_SchoolId_Name",
                table: "StudentCategories",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentBatches_SchoolId",
                table: "StudentBatches",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_SchoolId_Name",
                table: "Rooms",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_SchoolId_Name",
                table: "AcademicYears",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicTerms_SchoolId",
                table: "AcademicTerms",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_SchoolId_Code",
                table: "AcademicPrograms",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicPrograms_Schools_SchoolId",
                table: "AcademicPrograms",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicTerms_Schools_SchoolId",
                table: "AcademicTerms",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicYears_Schools_SchoolId",
                table: "AcademicYears",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rooms_Schools_SchoolId",
                table: "Rooms",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentBatches_Schools_SchoolId",
                table: "StudentBatches",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCategories_Schools_SchoolId",
                table: "StudentCategories",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentGroups_Schools_SchoolId",
                table: "StudentGroups",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademicPrograms_Schools_SchoolId",
                table: "AcademicPrograms");

            migrationBuilder.DropForeignKey(
                name: "FK_AcademicTerms_Schools_SchoolId",
                table: "AcademicTerms");

            migrationBuilder.DropForeignKey(
                name: "FK_AcademicYears_Schools_SchoolId",
                table: "AcademicYears");

            migrationBuilder.DropForeignKey(
                name: "FK_Rooms_Schools_SchoolId",
                table: "Rooms");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentBatches_Schools_SchoolId",
                table: "StudentBatches");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCategories_Schools_SchoolId",
                table: "StudentCategories");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentGroups_Schools_SchoolId",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_SchoolId",
                table: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_StudentCategories_SchoolId_Name",
                table: "StudentCategories");

            migrationBuilder.DropIndex(
                name: "IX_StudentBatches_SchoolId",
                table: "StudentBatches");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_SchoolId_Name",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_AcademicYears_SchoolId_Name",
                table: "AcademicYears");

            migrationBuilder.DropIndex(
                name: "IX_AcademicTerms_SchoolId",
                table: "AcademicTerms");

            migrationBuilder.DropIndex(
                name: "IX_AcademicPrograms_SchoolId_Code",
                table: "AcademicPrograms");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "StudentCategories");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "StudentBatches");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AcademicYears");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AcademicTerms");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AcademicPrograms");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCategories_Name",
                table: "StudentCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Name",
                table: "Rooms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_Name",
                table: "AcademicYears",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_Code",
                table: "AcademicPrograms",
                column: "Code",
                unique: true);
        }
    }
}

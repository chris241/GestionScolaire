using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionScolaire.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentPlanStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AssessmentPlans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "AssessmentPlans");
        }
    }
}

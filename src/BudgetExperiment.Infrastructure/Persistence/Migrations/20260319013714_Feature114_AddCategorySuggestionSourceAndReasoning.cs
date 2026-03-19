using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Feature114_AddCategorySuggestionSourceAndReasoning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reasoning",
                table: "CategorySuggestions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "CategorySuggestions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PatternMatch");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Reasoning",
                table: "CategorySuggestions");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "CategorySuggestions");
        }
    }
}

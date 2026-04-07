using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettingsShowSpendingHeatmap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowSpendingHeatmap",
                table: "UserSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowSpendingHeatmap",
                table: "UserSettings");
        }
    }
}

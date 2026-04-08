using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettingsKakeiboPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableKaizenMicroGoals",
                table: "UserSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowKakeiboCalendarBadges",
                table: "UserSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowMonthlyReflectionPrompts",
                table: "UserSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableKaizenMicroGoals",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "ShowKakeiboCalendarBadges",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "ShowMonthlyReflectionPrompts",
                table: "UserSettings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionKakeiboOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasSeenKakeiboSelectorTooltip",
                table: "UserSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "KakeiboOverride",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MonthlyReflections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    SavingsGoal = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    ActualSavings = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: true),
                    IntentionText = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: true),
                    GratitudeText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImprovementText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyReflections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReflections_UserId_Year_Month",
                table: "MonthlyReflections",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyReflections");

            migrationBuilder.DropColumn(
                name: "HasSeenKakeiboSelectorTooltip",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "KakeiboOverride",
                table: "Transactions");
        }
    }
}

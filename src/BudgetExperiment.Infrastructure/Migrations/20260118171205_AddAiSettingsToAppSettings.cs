using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSettingsToAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AiIsEnabled",
                table: "AppSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "AiMaxTokens",
                table: "AppSettings",
                type: "integer",
                nullable: false,
                defaultValue: 2000);

            migrationBuilder.AddColumn<string>(
                name: "AiModelName",
                table: "AppSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "llama3.2");

            migrationBuilder.AddColumn<string>(
                name: "AiOllamaEndpoint",
                table: "AppSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "http://localhost:11434");

            migrationBuilder.AddColumn<decimal>(
                name: "AiTemperature",
                table: "AppSettings",
                type: "numeric(3,2)",
                precision: 3,
                scale: 2,
                nullable: false,
                defaultValue: 0.3m);

            migrationBuilder.AddColumn<int>(
                name: "AiTimeoutSeconds",
                table: "AppSettings",
                type: "integer",
                nullable: false,
                defaultValue: 120);

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "AiIsEnabled", "AiMaxTokens", "AiModelName", "AiOllamaEndpoint", "AiTemperature", "AiTimeoutSeconds" },
                values: new object[] { true, 2000, "llama3.2", "http://localhost:11434", 0.3m, 120 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiIsEnabled",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AiMaxTokens",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AiModelName",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AiOllamaEndpoint",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AiTemperature",
                table: "AppSettings");

            migrationBuilder.DropColumn(
                name: "AiTimeoutSeconds",
                table: "AppSettings");
        }
    }
}

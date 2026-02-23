using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Feature060_AddLocationDataSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location_City",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location_Country",
                table: "Transactions",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Location_Latitude",
                table: "Transactions",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Location_Longitude",
                table: "Transactions",
                type: "numeric(9,6)",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location_PostalCode",
                table: "Transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Location_Source",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location_StateOrRegion",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableLocationData",
                table: "AppSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CustomReportLayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LayoutJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomReportLayouts", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AppSettings",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new string[0],
                values: new object[0]);

            migrationBuilder.CreateIndex(
                name: "IX_CustomReportLayouts_CreatedByUserId",
                table: "CustomReportLayouts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomReportLayouts_OwnerUserId",
                table: "CustomReportLayouts",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomReportLayouts_Scope",
                table: "CustomReportLayouts",
                column: "Scope");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomReportLayouts");

            migrationBuilder.DropColumn(
                name: "Location_City",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Location_Country",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Location_Latitude",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Location_Longitude",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Location_PostalCode",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Location_Source",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Location_StateOrRegion",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "EnableLocationData",
                table: "AppSettings");
        }
    }
}

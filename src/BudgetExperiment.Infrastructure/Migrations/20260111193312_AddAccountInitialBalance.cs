using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountInitialBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InitialBalance",
                table: "Accounts",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InitialBalanceCurrency",
                table: "Accounts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD");

            migrationBuilder.AddColumn<DateOnly>(
                name: "InitialBalanceDate",
                table: "Accounts",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Set InitialBalanceDate to CreatedAt date for existing accounts
            migrationBuilder.Sql(
                """
                UPDATE "Accounts"
                SET "InitialBalanceDate" = DATE("CreatedAt")
                WHERE "InitialBalanceDate" = '0001-01-01';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialBalance",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "InitialBalanceCurrency",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "InitialBalanceDate",
                table: "Accounts");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Feature125_TransactionClearedState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ClearedDate",
                table: "Transactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCleared",
                table: "Transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReconciliationRecordId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_IsCleared",
                table: "Transactions",
                columns: new[] { "AccountId", "IsCleared" },
                filter: "\"IsCleared\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ReconciliationRecordId",
                table: "Transactions",
                column: "ReconciliationRecordId",
                filter: "\"ReconciliationRecordId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_IsCleared",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ReconciliationRecordId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ClearedDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IsCleared",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReconciliationRecordId",
                table: "Transactions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Feature125_ReconciliationAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReconciliationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StatementBalance_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    StatementBalance_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ClearedBalance_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ClearedBalance_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatementBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Balance_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Balance_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatementBalances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationRecords_AccountId",
                table: "ReconciliationRecords",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationRecords_AccountId_StatementDate",
                table: "ReconciliationRecords",
                columns: new[] { "AccountId", "StatementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationRecords_Scope_OwnerUserId",
                table: "ReconciliationRecords",
                columns: new[] { "Scope", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_StatementBalances_AccountId",
                table: "StatementBalances",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StatementBalances_AccountId_IsCompleted",
                table: "StatementBalances",
                columns: new[] { "AccountId", "IsCompleted" },
                filter: "\"IsCompleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationRecords");

            migrationBuilder.DropTable(
                name: "StatementBalances");
        }
    }
}

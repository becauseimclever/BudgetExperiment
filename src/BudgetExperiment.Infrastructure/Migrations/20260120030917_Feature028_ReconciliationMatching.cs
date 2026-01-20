using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Feature028_ReconciliationMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReconciliationMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportedTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringInstanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AmountVariance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DateOffsetDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatches_RecurringTransactions_RecurringTransa~",
                        column: x => x.RecurringTransactionId,
                        principalTable: "RecurringTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatches_Transactions_ImportedTransactionId",
                        column: x => x.ImportedTransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_ImportedTransactionId",
                table: "ReconciliationMatches",
                column: "ImportedTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_ImportedTransactionId_RecurringTransa~",
                table: "ReconciliationMatches",
                columns: new[] { "ImportedTransactionId", "RecurringTransactionId", "RecurringInstanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_RecurringTransactionId_RecurringInsta~",
                table: "ReconciliationMatches",
                columns: new[] { "RecurringTransactionId", "RecurringInstanceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_Scope_OwnerUserId",
                table: "ReconciliationMatches",
                columns: new[] { "Scope", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_Status",
                table: "ReconciliationMatches",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationMatches");
        }
    }
}

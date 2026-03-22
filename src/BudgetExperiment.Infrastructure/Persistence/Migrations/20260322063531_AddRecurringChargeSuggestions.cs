using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringChargeSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecurringChargeSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SampleDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AverageAmountCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AverageAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DetectedFrequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DetectedInterval = table.Column<int>(type: "integer", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    MatchingTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    FirstOccurrence = table.Column<DateOnly>(type: "date", nullable: false),
                    LastOccurrence = table.Column<DateOnly>(type: "date", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AcceptedRecurringTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Scope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringChargeSuggestions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringChargeSuggestions_AccountId_Status",
                table: "RecurringChargeSuggestions",
                columns: new[] { "AccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringChargeSuggestions_NormalizedDescription_AccountId",
                table: "RecurringChargeSuggestions",
                columns: new[] { "NormalizedDescription", "AccountId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringChargeSuggestions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RuleSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Reasoning = table.Column<string>(type: "text", nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    SuggestedPattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SuggestedMatchType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SuggestedCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    OptimizedPattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConflictingRuleIds = table.Column<string>(type: "jsonb", nullable: false),
                    AffectedTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    SampleDescriptions = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DismissalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserFeedbackPositive = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSuggestions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleSuggestions_Pattern",
                table: "RuleSuggestions",
                column: "SuggestedPattern");

            migrationBuilder.CreateIndex(
                name: "IX_RuleSuggestions_Status",
                table: "RuleSuggestions",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RuleSuggestions_Type",
                table: "RuleSuggestions",
                columns: new[] { "Type", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RuleSuggestions");
        }
    }
}

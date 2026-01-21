using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Feature032_CategorySuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategorySuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestedName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SuggestedIcon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SuggestedColor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SuggestedType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    MerchantPatterns = table.Column<string>(type: "jsonb", nullable: false),
                    MatchingTransactionCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorySuggestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DismissedSuggestionPatterns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Pattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DismissedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DismissedSuggestionPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnedMerchantMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MerchantPattern = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LearnCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnedMerchantMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearnedMerchantMappings_BudgetCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BudgetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategorySuggestions_OwnerId_Status",
                table: "CategorySuggestions",
                columns: new[] { "OwnerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CategorySuggestions_OwnerId_SuggestedName_Status",
                table: "CategorySuggestions",
                columns: new[] { "OwnerId", "SuggestedName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DismissedSuggestionPatterns_OwnerId",
                table: "DismissedSuggestionPatterns",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DismissedSuggestionPatterns_Pattern_OwnerId",
                table: "DismissedSuggestionPatterns",
                columns: new[] { "Pattern", "OwnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnedMerchantMappings_CategoryId",
                table: "LearnedMerchantMappings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnedMerchantMappings_MerchantPattern_OwnerId",
                table: "LearnedMerchantMappings",
                columns: new[] { "MerchantPattern", "OwnerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearnedMerchantMappings_OwnerId",
                table: "LearnedMerchantMappings",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategorySuggestions");

            migrationBuilder.DropTable(
                name: "DismissedSuggestionPatterns");

            migrationBuilder.DropTable(
                name: "LearnedMerchantMappings");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Feature039_ManualReconciliationLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ReconciliationMatches",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Auto");

            migrationBuilder.CreateTable(
                name: "RecurringTransactionImportPatterns",
                columns: table => new
                {
                    Pattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecurringTransactionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTransactionImportPatterns", x => new { x.RecurringTransactionId, x.Pattern });
                    table.ForeignKey(
                        name: "FK_RecurringTransactionImportPatterns_RecurringTransactions_Re~",
                        column: x => x.RecurringTransactionId,
                        principalTable: "RecurringTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringTransactionImportPatterns");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ReconciliationMatches");
        }
    }
}

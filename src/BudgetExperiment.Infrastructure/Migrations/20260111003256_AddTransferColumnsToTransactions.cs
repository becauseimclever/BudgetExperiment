using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferColumnsToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TransferDirection",
                table: "Transactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransferId",
                table: "Transactions",
                column: "TransferId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_TransferId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransferDirection",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransferId",
                table: "Transactions");
        }
    }
}

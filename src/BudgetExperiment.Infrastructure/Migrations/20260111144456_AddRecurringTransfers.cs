using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecurringTransferId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RecurringTransferInstanceDate",
                table: "Transactions",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurringTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    Interval = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    MonthOfYear = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextOccurrence = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastGeneratedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringTransfers_Accounts_DestinationAccountId",
                        column: x => x.DestinationAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringTransfers_Accounts_SourceAccountId",
                        column: x => x.SourceAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTransferExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringTransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExceptionType = table.Column<int>(type: "integer", nullable: false),
                    ModifiedCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ModifiedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ModifiedDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ModifiedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTransferExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringTransferExceptions_RecurringTransfers_RecurringTra~",
                        column: x => x.RecurringTransferId,
                        principalTable: "RecurringTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RecurringTransferId",
                table: "Transactions",
                column: "RecurringTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransferExceptions_RecurringTransferId",
                table: "RecurringTransferExceptions",
                column: "RecurringTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransferExceptions_RecurringTransferId_OriginalDate",
                table: "RecurringTransferExceptions",
                columns: new[] { "RecurringTransferId", "OriginalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransfers_DestinationAccountId",
                table: "RecurringTransfers",
                column: "DestinationAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransfers_IsActive",
                table: "RecurringTransfers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransfers_NextOccurrence",
                table: "RecurringTransfers",
                column: "NextOccurrence");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransfers_SourceAccountId",
                table: "RecurringTransfers",
                column: "SourceAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_RecurringTransfers_RecurringTransferId",
                table: "Transactions",
                column: "RecurringTransferId",
                principalTable: "RecurringTransfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_RecurringTransfers_RecurringTransferId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "RecurringTransferExceptions");

            migrationBuilder.DropTable(
                name: "RecurringTransfers");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_RecurringTransferId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RecurringTransferId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RecurringTransferInstanceDate",
                table: "Transactions");
        }
    }
}

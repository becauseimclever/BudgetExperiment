using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "RecurringInstanceDate",
                table: "Transactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RecurringTransactionId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecurringTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_RecurringTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringTransactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTransactionExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecurringTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_RecurringTransactionExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringTransactionExceptions_RecurringTransactions_Recurr~",
                        column: x => x.RecurringTransactionId,
                        principalTable: "RecurringTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_RecurringTransactionId",
                table: "Transactions",
                column: "RecurringTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactionExceptions_RecurringTransactionId",
                table: "RecurringTransactionExceptions",
                column: "RecurringTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactionExceptions_RecurringTransactionId_Origi~",
                table: "RecurringTransactionExceptions",
                columns: new[] { "RecurringTransactionId", "OriginalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_AccountId",
                table: "RecurringTransactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_IsActive",
                table: "RecurringTransactions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_NextOccurrence",
                table: "RecurringTransactions",
                column: "NextOccurrence");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_RecurringTransactions_RecurringTransactionId",
                table: "Transactions",
                column: "RecurringTransactionId",
                principalTable: "RecurringTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_RecurringTransactions_RecurringTransactionId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "RecurringTransactionExceptions");

            migrationBuilder.DropTable(
                name: "RecurringTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_RecurringTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RecurringInstanceDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RecurringTransactionId",
                table: "Transactions");
        }
    }
}

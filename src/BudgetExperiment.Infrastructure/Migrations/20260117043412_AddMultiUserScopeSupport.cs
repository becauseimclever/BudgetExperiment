using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiUserScopeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BudgetCategories_Name",
                table: "BudgetCategories");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Transactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "RecurringTransfers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "RecurringTransfers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "RecurringTransfers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "RecurringTransactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "RecurringTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "RecurringTransactions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "BudgetGoals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "BudgetGoals",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "BudgetGoals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "BudgetCategories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "BudgetCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "BudgetCategories",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Accounts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "Accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Accounts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultScope = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AutoRealizePastDueItems = table.Column<bool>(type: "boolean", nullable: false),
                    PastDueLookbackDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    PreferredCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OwnerUserId",
                table: "Transactions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Scope",
                table: "Transactions",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransfers_OwnerUserId",
                table: "RecurringTransfers",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransfers_Scope",
                table: "RecurringTransfers",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_OwnerUserId",
                table: "RecurringTransactions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_Scope",
                table: "RecurringTransactions",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetGoals_OwnerUserId",
                table: "BudgetGoals",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetGoals_Scope",
                table: "BudgetGoals",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_Name",
                table: "BudgetCategories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_Name_Scope_OwnerUserId",
                table: "BudgetCategories",
                columns: new[] { "Name", "Scope", "OwnerUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_OwnerUserId",
                table: "BudgetCategories",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_Scope",
                table: "BudgetCategories",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_OwnerUserId",
                table: "Accounts",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Scope",
                table: "Accounts",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_OwnerUserId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Scope",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransfers_OwnerUserId",
                table: "RecurringTransfers");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransfers_Scope",
                table: "RecurringTransfers");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransactions_OwnerUserId",
                table: "RecurringTransactions");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransactions_Scope",
                table: "RecurringTransactions");

            migrationBuilder.DropIndex(
                name: "IX_BudgetGoals_OwnerUserId",
                table: "BudgetGoals");

            migrationBuilder.DropIndex(
                name: "IX_BudgetGoals_Scope",
                table: "BudgetGoals");

            migrationBuilder.DropIndex(
                name: "IX_BudgetCategories_Name",
                table: "BudgetCategories");

            migrationBuilder.DropIndex(
                name: "IX_BudgetCategories_Name_Scope_OwnerUserId",
                table: "BudgetCategories");

            migrationBuilder.DropIndex(
                name: "IX_BudgetCategories_OwnerUserId",
                table: "BudgetCategories");

            migrationBuilder.DropIndex(
                name: "IX_BudgetCategories_Scope",
                table: "BudgetCategories");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_OwnerUserId",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Scope",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "RecurringTransfers");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "RecurringTransfers");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "RecurringTransfers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "BudgetGoals");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "BudgetGoals");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "BudgetGoals");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "BudgetCategories");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "BudgetCategories");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "BudgetCategories");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_Name",
                table: "BudgetCategories",
                column: "Name",
                unique: true);
        }
    }
}

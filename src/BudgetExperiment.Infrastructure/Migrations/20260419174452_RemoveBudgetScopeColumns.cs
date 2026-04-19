using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBudgetScopeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Scope",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransfers_Scope",
                table: "RecurringTransfers");

            migrationBuilder.DropIndex(
                name: "IX_RecurringTransactions_Scope",
                table: "RecurringTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ReconciliationRecords_Scope_OwnerUserId",
                table: "ReconciliationRecords");

            migrationBuilder.DropIndex(
                name: "IX_ReconciliationMatches_Scope_OwnerUserId",
                table: "ReconciliationMatches");

            migrationBuilder.DropIndex(
                name: "IX_CustomReportLayouts_Scope",
                table: "CustomReportLayouts");

            migrationBuilder.DropIndex(
                name: "IX_BudgetGoals_Scope",
                table: "BudgetGoals");

            migrationBuilder.DropIndex(
                name: "IX_BudgetCategories_Name_Scope_OwnerUserId",
                table: "BudgetCategories");

            migrationBuilder.DropIndex(
                name: "IX_BudgetCategories_Scope",
                table: "BudgetCategories");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Scope",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "DefaultScope",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "RecurringTransfers");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "RecurringChargeSuggestions");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "ReconciliationRecords");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "ReconciliationMatches");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "CustomReportLayouts");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "BudgetGoals");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "BudgetCategories");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationRecords_OwnerUserId",
                table: "ReconciliationRecords",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_OwnerUserId",
                table: "ReconciliationMatches",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_Name_OwnerUserId",
                table: "BudgetCategories",
                columns: new[] { "Name", "OwnerUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReconciliationRecords_OwnerUserId",
                table: "ReconciliationRecords");

            migrationBuilder.DropIndex(
                name: "IX_ReconciliationMatches_OwnerUserId",
                table: "ReconciliationMatches");

            migrationBuilder.DropIndex(
                name: "IX_BudgetCategories_Name_OwnerUserId",
                table: "BudgetCategories");

            migrationBuilder.AddColumn<int>(
                name: "DefaultScope",
                table: "UserSettings",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "Transactions",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "RecurringTransfers",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "RecurringTransactions",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "RecurringChargeSuggestions",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "ReconciliationRecords",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "ReconciliationMatches",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "CustomReportLayouts",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "BudgetGoals",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "BudgetCategories",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "Accounts",
                type: "integer",
                nullable: true,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Scope",
                table: "Transactions",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransfers_Scope",
                table: "RecurringTransfers",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTransactions_Scope",
                table: "RecurringTransactions",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationRecords_Scope_OwnerUserId",
                table: "ReconciliationRecords",
                columns: new[] { "Scope", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_Scope_OwnerUserId",
                table: "ReconciliationMatches",
                columns: new[] { "Scope", "OwnerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomReportLayouts_Scope",
                table: "CustomReportLayouts",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetGoals_Scope",
                table: "BudgetGoals",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_Name_Scope_OwnerUserId",
                table: "BudgetCategories",
                columns: new[] { "Name", "Scope", "OwnerUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetCategories_Scope",
                table: "BudgetCategories",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Scope",
                table: "Accounts",
                column: "Scope");
        }
    }
}

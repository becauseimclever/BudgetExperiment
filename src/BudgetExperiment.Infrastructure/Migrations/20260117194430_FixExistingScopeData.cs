using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixExistingScopeData : Migration
    {
        /// <summary>
        /// The system user ID used for existing data created before multi-user support.
        /// </summary>
        private const string SystemUserId = "00000000-0000-0000-0000-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix existing Accounts: Set Scope to 'Shared' and CreatedByUserId to system user
            migrationBuilder.Sql($"""
                UPDATE "Accounts"
                SET "Scope" = 'Shared',
                    "CreatedByUserId" = '{SystemUserId}'::uuid
                WHERE "Scope" = '' OR "Scope" IS NULL;
                """);

            // Fix existing Transactions: Set Scope to 'Shared' and CreatedByUserId to system user
            migrationBuilder.Sql($"""
                UPDATE "Transactions"
                SET "Scope" = 'Shared',
                    "CreatedByUserId" = '{SystemUserId}'::uuid
                WHERE "Scope" = '' OR "Scope" IS NULL;
                """);

            // Fix existing RecurringTransactions: Set Scope to 'Shared' and CreatedByUserId to system user
            migrationBuilder.Sql($"""
                UPDATE "RecurringTransactions"
                SET "Scope" = 'Shared',
                    "CreatedByUserId" = '{SystemUserId}'::uuid
                WHERE "Scope" = '' OR "Scope" IS NULL;
                """);

            // Fix existing RecurringTransfers: Set Scope to 'Shared' and CreatedByUserId to system user
            migrationBuilder.Sql($"""
                UPDATE "RecurringTransfers"
                SET "Scope" = 'Shared',
                    "CreatedByUserId" = '{SystemUserId}'::uuid
                WHERE "Scope" = '' OR "Scope" IS NULL;
                """);

            // Fix existing BudgetCategories: Set Scope to 'Shared' and CreatedByUserId to system user
            migrationBuilder.Sql($"""
                UPDATE "BudgetCategories"
                SET "Scope" = 'Shared',
                    "CreatedByUserId" = '{SystemUserId}'::uuid
                WHERE "Scope" = '' OR "Scope" IS NULL;
                """);

            // Fix existing BudgetGoals: Set Scope to 'Shared' and CreatedByUserId to system user
            migrationBuilder.Sql($"""
                UPDATE "BudgetGoals"
                SET "Scope" = 'Shared',
                    "CreatedByUserId" = '{SystemUserId}'::uuid
                WHERE "Scope" = '' OR "Scope" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration fixes data; reverting would set data back to invalid state
            // which we don't want to do. No-op for down migration.
        }
    }
}

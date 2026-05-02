using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Feature163_EncryptedColumnStorageCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Amount",
                table: "Transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "IntentionText",
                table: "MonthlyReflections",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(280)",
                oldMaxLength: 280,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImprovementText",
                table: "MonthlyReflections",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GratitudeText",
                table: "MonthlyReflections",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "KaizenGoals",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Pattern",
                table: "CategorizationRules",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Accounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Transactions"
                        WHERE "Amount" LIKE 'enc::%'
                           OR "Description" LIKE 'enc::%')
                    OR EXISTS (
                        SELECT 1
                        FROM "Accounts"
                        WHERE "Name" LIKE 'enc::%')
                    OR EXISTS (
                        SELECT 1
                        FROM "CategorizationRules"
                        WHERE "Pattern" LIKE 'enc::%')
                    OR EXISTS (
                        SELECT 1
                        FROM "KaizenGoals"
                        WHERE "Description" LIKE 'enc::%')
                    OR EXISTS (
                        SELECT 1
                        FROM "MonthlyReflections"
                        WHERE COALESCE("IntentionText", '') LIKE 'enc::%'
                           OR COALESCE("GratitudeText", '') LIKE 'enc::%'
                           OR COALESCE("ImprovementText", '') LIKE 'enc::%')
                    THEN
                        RAISE EXCEPTION 'Feature 163 rollback is blocked because encrypted ciphertext rows exist. Downgrade requires restore from a pre-encryption backup or a validated data migration plan.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Transactions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "IntentionText",
                table: "MonthlyReflections",
                type: "character varying(280)",
                maxLength: 280,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImprovementText",
                table: "MonthlyReflections",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GratitudeText",
                table: "MonthlyReflections",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "KaizenGoals",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Pattern",
                table: "CategorizationRules",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Accounts",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

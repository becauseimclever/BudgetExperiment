using System;

using BudgetExperiment.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetExperiment.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(BudgetDbContext))]
    [Migration("20260412102000_AddAiBackendTypeToAppSettings")]
    public partial class AddAiBackendTypeToAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiBackendType",
                table: "AppSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiBackendType",
                table: "AppSettings");
        }
    }
}

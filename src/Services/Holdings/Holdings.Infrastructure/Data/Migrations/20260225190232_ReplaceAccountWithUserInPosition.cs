using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holdings.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAccountWithUserInPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Accounts_AccountId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_AccountId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Positions",
                newName: "UserId");

            migrationBuilder.AlterColumn<string>(
                name: "Ticker",
                table: "Positions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Positions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "AveragePurchasePrice",
                table: "Positions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AddColumn<decimal>(
                name: "CashBalance",
                table: "Accounts",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UserId_Ticker",
                table: "Positions",
                columns: new[] { "UserId", "Ticker" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Positions_UserId_Ticker",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "CashBalance",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Positions",
                newName: "AccountId");

            migrationBuilder.AlterColumn<string>(
                name: "Ticker",
                table: "Positions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Positions",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<decimal>(
                name: "AveragePurchasePrice",
                table: "Positions",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Accounts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Positions_AccountId",
                table: "Positions",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Accounts_AccountId",
                table: "Positions",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

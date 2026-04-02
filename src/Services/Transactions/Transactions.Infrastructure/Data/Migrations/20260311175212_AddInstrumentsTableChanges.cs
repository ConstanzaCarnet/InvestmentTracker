using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transactions.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentsTableChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CedearRatio",
                table: "Instruments");

            migrationBuilder.AddColumn<string>(
                name: "AssetType",
                table: "Instruments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Instruments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "Ratio",
                table: "Instruments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssetType",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "Ratio",
                table: "Instruments");

            migrationBuilder.AddColumn<int>(
                name: "CedearRatio",
                table: "Instruments",
                type: "int",
                nullable: true);
        }
    }
}

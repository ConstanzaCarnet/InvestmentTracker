using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Transactions.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentsChangesTwo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Ratio",
                table: "Instruments",
                newName: "ConversionRatio");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Instruments",
                newName: "LastUpdated");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "Instruments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ConversionRatio",
                table: "Instruments",
                newName: "Ratio");
        }
    }
}

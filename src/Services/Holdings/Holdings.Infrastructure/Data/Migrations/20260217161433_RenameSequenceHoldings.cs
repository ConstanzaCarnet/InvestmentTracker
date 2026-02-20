using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holdings.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameSequenceHoldings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastProcessedVersion",
                table: "Positions",
                newName: "LastProcessedSequenceNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastProcessedSequenceNumber",
                table: "Positions",
                newName: "LastProcessedVersion");
        }
    }
}

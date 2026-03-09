using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holdings.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePositionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalInvestedAmount",
                table: "Positions");

            migrationBuilder.AddColumn<decimal>(
                name: "InvestedAmount",
                table: "Positions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBoughtAmount",
                table: "Positions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBoughtQuantity",
                table: "Positions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSoldAmount",
                table: "Positions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSoldQuantity",
                table: "Positions",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvestedAmount",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "TotalBoughtAmount",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "TotalBoughtQuantity",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "TotalSoldAmount",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "TotalSoldQuantity",
                table: "Positions");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalInvestedAmount",
                table: "Positions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabaseGirIDMS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VarianceReason",
                table: "StockCheckDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrImageUrl",
                table: "Slots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrImageUrl",
                table: "Lots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrImageUrl",
                table: "Boxes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VarianceReason",
                table: "StockCheckDetails");

            migrationBuilder.DropColumn(
                name: "QrImageUrl",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "QrImageUrl",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "QrImageUrl",
                table: "Boxes");
        }
    }
}

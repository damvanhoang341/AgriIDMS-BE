using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneRackSlotDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FloorAreaM2",
                table: "Zones",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthM",
                table: "Zones",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthM",
                table: "Zones",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeightCm",
                table: "Slots",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthCm",
                table: "Slots",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolumeM3",
                table: "Slots",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthCm",
                table: "Slots",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FloorAreaM2",
                table: "Racks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthM",
                table: "Racks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthM",
                table: "Racks",
                type: "decimal(18,2)",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FloorAreaM2",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "LengthM",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "WidthM",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "HeightCm",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "LengthCm",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "VolumeM3",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "WidthCm",
                table: "Slots");

            migrationBuilder.DropColumn(
                name: "FloorAreaM2",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "LengthM",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "WidthM",
                table: "Racks");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColdStorageTimeWarehouseAndBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinColdStorageHours",
                table: "Warehouses",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlacedInColdAt",
                table: "Boxes",
                type: "datetime2",
                nullable: true);

            // Mặc định 48h cho kho lạnh hiện có
            migrationBuilder.Sql(
                "UPDATE Warehouses SET MinColdStorageHours = 48 WHERE TitleWarehouse = N'Cold' AND MinColdStorageHours IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinColdStorageHours",
                table: "Warehouses");

            migrationBuilder.DropColumn(
                name: "PlacedInColdAt",
                table: "Boxes");
        }
    }
}

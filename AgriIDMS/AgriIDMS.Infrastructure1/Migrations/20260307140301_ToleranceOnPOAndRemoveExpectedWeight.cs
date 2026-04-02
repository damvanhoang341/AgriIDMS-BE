using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ToleranceOnPOAndRemoveExpectedWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TolerancePercent",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ExpectedWeight",
                table: "GoodsReceiptDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "TolerancePercent",
                table: "PurchaseOrderDetails",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 2m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TolerancePercent",
                table: "PurchaseOrderDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "TolerancePercent",
                table: "GoodsReceipts",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 2m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }
    }
}

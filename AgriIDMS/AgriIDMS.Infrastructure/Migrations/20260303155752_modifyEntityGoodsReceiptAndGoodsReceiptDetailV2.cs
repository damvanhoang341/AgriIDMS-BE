using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modifyEntityGoodsReceiptAndGoodsReceiptDetailV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreOrderQuantity",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "TotalActualQuantity",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ActualQuantity",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "EstimatedQuantity",
                table: "GoodsReceiptDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "TolerancePercent",
                table: "GoodsReceipts",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLossWeight",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OrderedWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RejectWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UsableWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TolerancePercent",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "TotalLossWeight",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "OrderedWeight",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "RejectWeight",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "UsableWeight",
                table: "GoodsReceiptDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "PreOrderQuantity",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalActualQuantity",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualQuantity",
                table: "GoodsReceiptDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedQuantity",
                table: "GoodsReceiptDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}

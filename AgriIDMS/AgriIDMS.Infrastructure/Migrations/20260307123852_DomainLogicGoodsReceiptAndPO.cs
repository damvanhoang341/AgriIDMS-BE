using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DomainLogicGoodsReceiptAndPO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptDetails_AspNetUsers_InspectedBy",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptDetails_InspectedBy",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "TotalLossWeight",
                table: "GoodsReceipts");

            // Add ExpectedWeight, copy from OrderedWeight, then drop OrderedWeight
            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);
            migrationBuilder.Sql("UPDATE GoodsReceiptDetails SET ExpectedWeight = OrderedWeight WHERE ExpectedWeight IS NULL");
            migrationBuilder.AlterColumn<decimal>(
                name: "ExpectedWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.DropColumn(
                name: "OrderedWeight",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "RejectWeight",
                table: "GoodsReceiptDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "ReceivedWeight",
                table: "PurchaseOrderDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TolerancePercent",
                table: "GoodsReceipts",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 2m,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AddColumn<int>(
                name: "PurchaseOrderId",
                table: "GoodsReceipts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptCode",
                table: "GoodsReceipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
            migrationBuilder.Sql("UPDATE GoodsReceipts SET ReceiptCode = CONCAT('GR-', YEAR(CreatedAt), '-', FORMAT(Id, '00000')) WHERE ReceiptCode IS NULL");
            migrationBuilder.AlterColumn<string>(
                name: "ReceiptCode",
                table: "GoodsReceipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy",
                table: "GoodsReceipts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UsableWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                table: "GoodsReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_ReceiptCode",
                table: "GoodsReceipts",
                column: "ReceiptCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                table: "GoodsReceipts",
                column: "PurchaseOrderId",
                principalTable: "PurchaseOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_ReceiptCode",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ReceivedWeight",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ReceiptCode",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "GoodsReceipts");

            migrationBuilder.AddColumn<decimal>(
                name: "OrderedWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.Sql("UPDATE GoodsReceiptDetails SET OrderedWeight = ExpectedWeight");
            migrationBuilder.AddColumn<decimal>(
                name: "RejectWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.DropColumn(
                name: "ExpectedWeight",
                table: "GoodsReceiptDetails");

            migrationBuilder.AlterColumn<decimal>(
                name: "TolerancePercent",
                table: "GoodsReceipts",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 2m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLossWeight",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "UsableWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptDetails_InspectedBy",
                table: "GoodsReceiptDetails",
                column: "InspectedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptDetails_AspNetUsers_InspectedBy",
                table: "GoodsReceiptDetails",
                column: "InspectedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

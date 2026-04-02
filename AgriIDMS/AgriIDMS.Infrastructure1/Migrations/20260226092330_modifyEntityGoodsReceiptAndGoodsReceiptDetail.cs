using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modifyEntityGoodsReceiptAndGoodsReceiptDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lots_GoodsReceiptDetails_GoodsReceiptDetailId",
                table: "Lots");

            migrationBuilder.DropForeignKey(
                name: "FK_Lots_ProductVariants_ProductVariantId",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_ExpiryDate",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_GoodsReceiptDetailId",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_ProductVariantId",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_ProductVariantId_ExpiryDate_Status",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_Status",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptDetails_ExpiryDate",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptDetails_QCResult",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "GoodsReceiptDetails");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Lots",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalEstimatedQuantity",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalActualQuantity",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "GoodsReceipts",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "GoodsReceipts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "GoodsReceipts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossWeight",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TareWeight",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransportCompany",
                table: "GoodsReceipts",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleNumber",
                table: "GoodsReceipts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "QCResult",
                table: "GoodsReceiptDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateIndex(
                name: "IX_Lots_GoodsReceiptDetailId",
                table: "Lots",
                column: "GoodsReceiptDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_Status_ExpiryDate",
                table: "Lots",
                columns: new[] { "Status", "ExpiryDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Lot_RemainingQuantity",
                table: "Lots",
                sql: "[RemainingQuantity] >= 0 AND [RemainingQuantity] <= [TotalQuantity]");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_ReceivedDate",
                table: "GoodsReceipts",
                column: "ReceivedDate");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_Status",
                table: "GoodsReceipts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipts_WarehouseId_Status",
                table: "GoodsReceipts",
                columns: new[] { "WarehouseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptDetails_GoodsReceiptId_ProductVariantId",
                table: "GoodsReceiptDetails",
                columns: new[] { "GoodsReceiptId", "ProductVariantId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Lots_GoodsReceiptDetails_GoodsReceiptDetailId",
                table: "Lots",
                column: "GoodsReceiptDetailId",
                principalTable: "GoodsReceiptDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lots_GoodsReceiptDetails_GoodsReceiptDetailId",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_GoodsReceiptDetailId",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_Status_ExpiryDate",
                table: "Lots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Lot_RemainingQuantity",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_ReceivedDate",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_Status",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_WarehouseId_Status",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptDetails_GoodsReceiptId_ProductVariantId",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "GrossWeight",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "TareWeight",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "TransportCompany",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "VehicleNumber",
                table: "GoodsReceipts");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Lots",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "Lots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalEstimatedQuantity",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalActualQuantity",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "GoodsReceipts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "GoodsReceipts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "QCResult",
                table: "GoodsReceiptDetails",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "GoodsReceiptDetails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Lots_ExpiryDate",
                table: "Lots",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_GoodsReceiptDetailId",
                table: "Lots",
                column: "GoodsReceiptDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lots_ProductVariantId",
                table: "Lots",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Lots_ProductVariantId_ExpiryDate_Status",
                table: "Lots",
                columns: new[] { "ProductVariantId", "ExpiryDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Lots_Status",
                table: "Lots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptDetails_ExpiryDate",
                table: "GoodsReceiptDetails",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptDetails_QCResult",
                table: "GoodsReceiptDetails",
                column: "QCResult");

            migrationBuilder.AddForeignKey(
                name: "FK_Lots_GoodsReceiptDetails_GoodsReceiptDetailId",
                table: "Lots",
                column: "GoodsReceiptDetailId",
                principalTable: "GoodsReceiptDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Lots_ProductVariants_ProductVariantId",
                table: "Lots",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

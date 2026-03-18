using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeUsableWeightNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExportReceiptId",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

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
                name: "IX_InventoryTransactions_ExportReceiptId",
                table: "InventoryTransactions",
                column: "ExportReceiptId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_ExportReceipts_ExportReceiptId",
                table: "InventoryTransactions",
                column: "ExportReceiptId",
                principalTable: "ExportReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_ExportReceipts_ExportReceiptId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ExportReceiptId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ExportReceiptId",
                table: "InventoryTransactions");

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
        }
    }
}

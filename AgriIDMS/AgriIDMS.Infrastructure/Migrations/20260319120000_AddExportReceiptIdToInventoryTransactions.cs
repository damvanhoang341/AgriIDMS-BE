using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExportReceiptIdToInventoryTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExportReceiptId",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

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
        }
    }
}


using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class modifydatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptDetails_Products_ProductId",
                table: "GoodsReceiptDetails");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "GoodsReceiptDetails",
                newName: "ProductVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceiptDetails_ProductId",
                table: "GoodsReceiptDetails",
                newName: "IX_GoodsReceiptDetails_ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptDetails_ProductVariants_ProductVariantId",
                table: "GoodsReceiptDetails",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptDetails_ProductVariants_ProductVariantId",
                table: "GoodsReceiptDetails");

            migrationBuilder.RenameColumn(
                name: "ProductVariantId",
                table: "GoodsReceiptDetails",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_GoodsReceiptDetails_ProductVariantId",
                table: "GoodsReceiptDetails",
                newName: "IX_GoodsReceiptDetails_ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptDetails_Products_ProductId",
                table: "GoodsReceiptDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

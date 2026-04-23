using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260423213000_AddProductVariantIdToLots")]
    public partial class AddProductVariantIdToLots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "Lots",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE l
                SET l.ProductVariantId = grd.ProductVariantId
                FROM Lots l
                INNER JOIN GoodsReceiptDetails grd ON grd.Id = l.GoodsReceiptDetailId
                WHERE l.ProductVariantId IS NULL;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Lots WHERE ProductVariantId IS NULL)
                BEGIN
                    THROW 50001, 'Cannot set Lots.ProductVariantId to NOT NULL because some rows could not be backfilled.', 1;
                END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "ProductVariantId",
                table: "Lots",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lots_ProductVariantId",
                table: "Lots",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lots_ProductVariants_ProductVariantId",
                table: "Lots",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lots_ProductVariants_ProductVariantId",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_Lots_ProductVariantId",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "Lots");
        }
    }
}

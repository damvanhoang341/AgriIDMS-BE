using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260426044500_DropLegacyUniqueIndexOnGoodsReceiptDetailVariant")]
    public partial class DropLegacyUniqueIndexOnGoodsReceiptDetailVariant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.objects o ON i.object_id = o.object_id
    WHERE i.name = 'IX_GoodsReceiptDetails_GoodsReceiptId_ProductVariantId'
      AND o.name = 'GoodsReceiptDetails'
      AND SCHEMA_NAME(o.schema_id) = 'dbo'
)
BEGIN
    DROP INDEX [IX_GoodsReceiptDetails_GoodsReceiptId_ProductVariantId] ON [dbo].[GoodsReceiptDetails];
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.objects o ON i.object_id = o.object_id
    WHERE i.name = 'IX_GoodsReceiptDetails_GoodsReceiptId_ProductVariantId'
      AND o.name = 'GoodsReceiptDetails'
      AND SCHEMA_NAME(o.schema_id) = 'dbo'
)
AND COL_LENGTH('dbo.GoodsReceiptDetails', 'GoodsReceiptId') IS NOT NULL
AND COL_LENGTH('dbo.GoodsReceiptDetails', 'ProductVariantId') IS NOT NULL
BEGIN
    CREATE UNIQUE INDEX [IX_GoodsReceiptDetails_GoodsReceiptId_ProductVariantId]
    ON [dbo].[GoodsReceiptDetails]([GoodsReceiptId], [ProductVariantId]);
END
");
        }
    }
}

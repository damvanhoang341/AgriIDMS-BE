using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260426043000_MakeGoodsReceiptDetailProductVariantNullable")]
    public partial class MakeGoodsReceiptDetailProductVariantNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.GoodsReceiptDetails', 'ProductVariantId') IS NOT NULL
BEGIN
    DECLARE @is_nullable bit;
    SELECT @is_nullable = c.is_nullable
    FROM sys.columns c
    INNER JOIN sys.objects o ON o.object_id = c.object_id
    WHERE o.name = 'GoodsReceiptDetails'
      AND SCHEMA_NAME(o.schema_id) = 'dbo'
      AND c.name = 'ProductVariantId';

    IF (@is_nullable = 0)
    BEGIN
        EXEC('ALTER TABLE [dbo].[GoodsReceiptDetails] ALTER COLUMN [ProductVariantId] int NULL;');
    END
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op to avoid forcing NOT NULL back on drifted databases.
        }
    }
}

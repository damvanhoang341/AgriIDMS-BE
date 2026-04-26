using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260426104700_EnsureGoodsReceiptDetailsProductIdExists")]
    public partial class EnsureGoodsReceiptDetailsProductIdExists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ProductId') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[GoodsReceiptDetails]
                    ADD [ProductId] int NOT NULL
                        CONSTRAINT [DF_GoodsReceiptDetails_ProductId_Repair] DEFAULT(0);
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ProductId') IS NOT NULL
                BEGIN
                    UPDATE grd
                    SET grd.[ProductId] = pv.[ProductId]
                    FROM [dbo].[GoodsReceiptDetails] grd
                    INNER JOIN [dbo].[ProductVariants] pv ON pv.[Id] = grd.[ProductVariantId]
                    WHERE grd.[ProductId] = 0
                      AND grd.[ProductVariantId] IS NOT NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_GoodsReceiptDetails_ProductId'
                      AND [object_id] = OBJECT_ID(N'[dbo].[GoodsReceiptDetails]')
                )
                AND COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ProductId') IS NOT NULL
                BEGIN
                    CREATE INDEX [IX_GoodsReceiptDetails_ProductId]
                    ON [dbo].[GoodsReceiptDetails]([ProductId]);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_GoodsReceiptDetails_Products_ProductId'
                )
                AND COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ProductId') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[GoodsReceiptDetails] WITH CHECK
                    ADD CONSTRAINT [FK_GoodsReceiptDetails_Products_ProductId]
                    FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_GoodsReceiptDetails_Products_ProductId'
                )
                BEGIN
                    ALTER TABLE [dbo].[GoodsReceiptDetails]
                    DROP CONSTRAINT [FK_GoodsReceiptDetails_Products_ProductId];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_GoodsReceiptDetails_ProductId'
                      AND [object_id] = OBJECT_ID(N'[dbo].[GoodsReceiptDetails]')
                )
                BEGIN
                    DROP INDEX [IX_GoodsReceiptDetails_ProductId] ON [dbo].[GoodsReceiptDetails];
                END
                """);
        }
    }
}


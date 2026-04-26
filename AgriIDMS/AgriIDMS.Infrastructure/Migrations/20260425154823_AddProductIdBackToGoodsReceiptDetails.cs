using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdBackToGoodsReceiptDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ProductId') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[GoodsReceiptDetails]
                    ADD [ProductId] int NOT NULL
                        CONSTRAINT [DF_GoodsReceiptDetails_ProductId] DEFAULT(0);
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ProductId') IS NOT NULL
                BEGIN
                    EXEC(N'
                        UPDATE grd
                        SET grd.[ProductId] = pv.[ProductId]
                        FROM [dbo].[GoodsReceiptDetails] grd
                        INNER JOIN [dbo].[ProductVariants] pv ON pv.[Id] = grd.[ProductVariantId]
                        WHERE grd.[ProductId] = 0;
                    ');
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_GoodsReceiptDetails_ProductId'
                      AND [object_id] = OBJECT_ID(N'[dbo].[GoodsReceiptDetails]')
                )
                BEGIN
                    CREATE INDEX [IX_GoodsReceiptDetails_ProductId]
                    ON [dbo].[GoodsReceiptDetails]([ProductId]);
                END

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_GoodsReceiptDetails_Products_ProductId'
                )
                AND EXISTS (
                    SELECT 1
                    FROM sys.columns
                    WHERE [object_id] = OBJECT_ID(N'[dbo].[GoodsReceiptDetails]')
                      AND [name] = N'ProductId'
                )
                BEGIN
                    EXEC(N'
                        ALTER TABLE [dbo].[GoodsReceiptDetails] WITH CHECK
                        ADD CONSTRAINT [FK_GoodsReceiptDetails_Products_ProductId]
                        FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION;
                    ');
                END
                """);
        }

        /// <inheritdoc />
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

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_GoodsReceiptDetails_ProductId'
                      AND [object_id] = OBJECT_ID(N'[dbo].[GoodsReceiptDetails]')
                )
                BEGIN
                    DROP INDEX [IX_GoodsReceiptDetails_ProductId] ON [dbo].[GoodsReceiptDetails];
                END

                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ProductId') IS NOT NULL
                BEGIN
                    DECLARE @defaultConstraintName nvarchar(128);
                    SELECT @defaultConstraintName = dc.[name]
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c
                        ON c.[default_object_id] = dc.[object_id]
                    WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[GoodsReceiptDetails]')
                      AND c.[name] = N'ProductId';

                    IF @defaultConstraintName IS NOT NULL
                    BEGIN
                        EXEC(N'ALTER TABLE [dbo].[GoodsReceiptDetails] DROP CONSTRAINT [' + @defaultConstraintName + N']');
                    END

                    ALTER TABLE [dbo].[GoodsReceiptDetails]
                    DROP COLUMN [ProductId];
                END
                """);
        }
    }
}

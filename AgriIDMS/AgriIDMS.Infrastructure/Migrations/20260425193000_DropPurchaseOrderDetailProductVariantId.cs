using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260425193000_DropPurchaseOrderDetailProductVariantId")]
    public partial class DropPurchaseOrderDetailProductVariantId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductVariantId') IS NOT NULL
                BEGIN
                    DECLARE @dropFkSql nvarchar(max) = N'';
                    DECLARE @dropIdxSql nvarchar(max) = N'';
                    DECLARE @dropDfSql nvarchar(max) = N'';

                    SELECT @dropFkSql = @dropFkSql +
                        N'ALTER TABLE [dbo].[PurchaseOrderDetails] DROP CONSTRAINT [' + fk.name + N'];'
                    FROM sys.foreign_keys fk
                    INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
                    WHERE fk.parent_object_id = OBJECT_ID(N'[dbo].[PurchaseOrderDetails]')
                      AND c.name = N'ProductVariantId';

                    IF @dropFkSql <> N'' EXEC sp_executesql @dropFkSql;

                    SELECT @dropIdxSql = @dropIdxSql +
                        N'DROP INDEX [' + i.name + N'] ON [dbo].[PurchaseOrderDetails];'
                    FROM sys.indexes i
                    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                    INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE i.object_id = OBJECT_ID(N'[dbo].[PurchaseOrderDetails]')
                      AND i.is_primary_key = 0
                      AND i.is_unique_constraint = 0
                      AND c.name = N'ProductVariantId';

                    IF @dropIdxSql <> N'' EXEC sp_executesql @dropIdxSql;

                    SELECT @dropDfSql = N'ALTER TABLE [dbo].[PurchaseOrderDetails] DROP CONSTRAINT [' + dc.name + N'];'
                    FROM sys.default_constraints dc
                    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
                    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[PurchaseOrderDetails]')
                      AND c.name = N'ProductVariantId';

                    IF @dropDfSql IS NOT NULL AND @dropDfSql <> N'' EXEC sp_executesql @dropDfSql;

                    ALTER TABLE [dbo].[PurchaseOrderDetails] DROP COLUMN [ProductVariantId];
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductVariantId') IS NULL
                    ALTER TABLE [dbo].[PurchaseOrderDetails] ADD [ProductVariantId] int NULL;
                """);
        }
    }
}


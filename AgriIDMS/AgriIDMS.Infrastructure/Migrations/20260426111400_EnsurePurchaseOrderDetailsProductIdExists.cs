using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260426111400_EnsurePurchaseOrderDetailsProductIdExists")]
    public partial class EnsurePurchaseOrderDetailsProductIdExists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductId') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[PurchaseOrderDetails]
                    ADD [ProductId] int NOT NULL
                        CONSTRAINT [DF_PurchaseOrderDetails_ProductId_Repair] DEFAULT(0);
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductId') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductVariantId') IS NOT NULL
                BEGIN
                    EXEC(N'
                        UPDATE pod
                        SET pod.[ProductId] = pv.[ProductId]
                        FROM [dbo].[PurchaseOrderDetails] pod
                        INNER JOIN [dbo].[ProductVariants] pv ON pv.[Id] = pod.[ProductVariantId]
                        WHERE pod.[ProductId] = 0;
                    ');
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductId') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductVariantId') IS NOT NULL
                BEGIN
                    EXEC(N'
                        UPDATE pod
                        SET pod.[ProductId] = pv.[ProductId]
                        FROM [dbo].[PurchaseOrderDetails] pod
                        INNER JOIN [dbo].[ProductVariants] pv ON pv.[Id] = pod.[ProductVariantId]
                        LEFT JOIN [dbo].[Products] p ON p.[Id] = pod.[ProductId]
                        WHERE p.[Id] IS NULL;
                    ');
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductId') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'SupplierPlanDetailId') IS NOT NULL
                   AND OBJECT_ID(N'[dbo].[PurchaseOrderSupplierPlanDetails]', N'U') IS NOT NULL
                BEGIN
                    EXEC(N'
                        UPDATE pod
                        SET pod.[ProductId] = spd.[ProductId]
                        FROM [dbo].[PurchaseOrderDetails] pod
                        INNER JOIN [dbo].[PurchaseOrderSupplierPlanDetails] spd
                            ON spd.[Id] = pod.[SupplierPlanDetailId]
                        WHERE pod.[ProductId] = 0;
                    ');
                END
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductId') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'SupplierPlanDetailId') IS NOT NULL
                   AND OBJECT_ID(N'[dbo].[PurchaseOrderSupplierPlanDetails]', N'U') IS NOT NULL
                BEGIN
                    EXEC(N'
                        UPDATE pod
                        SET pod.[ProductId] = spd.[ProductId]
                        FROM [dbo].[PurchaseOrderDetails] pod
                        INNER JOIN [dbo].[PurchaseOrderSupplierPlanDetails] spd
                            ON spd.[Id] = pod.[SupplierPlanDetailId]
                        LEFT JOIN [dbo].[Products] p ON p.[Id] = pod.[ProductId]
                        WHERE p.[Id] IS NULL;
                    ');
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_PurchaseOrderDetails_ProductId'
                      AND [object_id] = OBJECT_ID(N'[dbo].[PurchaseOrderDetails]')
                )
                AND COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductId') IS NOT NULL
                BEGIN
                    CREATE INDEX [IX_PurchaseOrderDetails_ProductId]
                    ON [dbo].[PurchaseOrderDetails]([ProductId]);
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_PurchaseOrderDetails_Products_ProductId'
                )
                AND COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductId') IS NOT NULL
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM [dbo].[PurchaseOrderDetails] pod
                        LEFT JOIN [dbo].[Products] p ON p.[Id] = pod.[ProductId]
                        WHERE p.[Id] IS NULL
                    )
                    BEGIN
                        ALTER TABLE [dbo].[PurchaseOrderDetails] WITH NOCHECK
                        ADD CONSTRAINT [FK_PurchaseOrderDetails_Products_ProductId]
                        FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION;
                    END
                    ELSE
                    BEGIN
                        ALTER TABLE [dbo].[PurchaseOrderDetails] WITH CHECK
                        ADD CONSTRAINT [FK_PurchaseOrderDetails_Products_ProductId]
                        FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION;
                    END
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE [name] = N'FK_PurchaseOrderDetails_Products_ProductId'
                )
                BEGIN
                    ALTER TABLE [dbo].[PurchaseOrderDetails]
                    DROP CONSTRAINT [FK_PurchaseOrderDetails_Products_ProductId];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE [name] = N'IX_PurchaseOrderDetails_ProductId'
                      AND [object_id] = OBJECT_ID(N'[dbo].[PurchaseOrderDetails]')
                )
                BEGIN
                    DROP INDEX [IX_PurchaseOrderDetails_ProductId] ON [dbo].[PurchaseOrderDetails];
                END
                """);
        }
    }
}


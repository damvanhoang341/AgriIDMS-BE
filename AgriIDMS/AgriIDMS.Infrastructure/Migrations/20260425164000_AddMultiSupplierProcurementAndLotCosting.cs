using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using AgriIDMS.Infrastructure.Data;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260425164000_AddMultiSupplierProcurementAndLotCosting")]
    public partial class AddMultiSupplierProcurementAndLotCosting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[PurchaseOrders]', N'ProcurementMode') IS NULL
                    ALTER TABLE [dbo].[PurchaseOrders] ADD [ProcurementMode] int NOT NULL CONSTRAINT [DF_PurchaseOrders_ProcurementMode] DEFAULT(1);

                IF OBJECT_ID(N'[dbo].[PurchaseOrderSupplierPlans]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PurchaseOrderSupplierPlans] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [PurchaseOrderId] int NOT NULL,
                        [SupplierId] int NOT NULL,
                        [OrderDate] datetime2 NOT NULL,
                        [Notes] nvarchar(500) NULL,
                        CONSTRAINT [PK_PurchaseOrderSupplierPlans] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PurchaseOrderSupplierPlans_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [dbo].[PurchaseOrders]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_PurchaseOrderSupplierPlans_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [dbo].[Suppliers]([Id]) ON DELETE NO ACTION
                    );
                    CREATE INDEX [IX_PurchaseOrderSupplierPlans_PurchaseOrderId_SupplierId] ON [dbo].[PurchaseOrderSupplierPlans]([PurchaseOrderId], [SupplierId]);
                END

                IF OBJECT_ID(N'[dbo].[PurchaseOrderSupplierPlanDetails]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PurchaseOrderSupplierPlanDetails] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [SupplierPlanId] int NOT NULL,
                        [ProductId] int NOT NULL,
                        [OrderedWeight] decimal(18,3) NOT NULL,
                        [UnitPriceAtOrder] decimal(18,2) NOT NULL,
                        [PriceDate] datetime2 NOT NULL,
                        [TolerancePercent] decimal(5,2) NOT NULL CONSTRAINT [DF_PurchaseOrderSupplierPlanDetails_TolerancePercent] DEFAULT(0),
                        CONSTRAINT [PK_PurchaseOrderSupplierPlanDetails] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_PurchaseOrderSupplierPlanDetails_PurchaseOrderSupplierPlans_SupplierPlanId] FOREIGN KEY ([SupplierPlanId]) REFERENCES [dbo].[PurchaseOrderSupplierPlans]([Id]) ON DELETE CASCADE,
                        CONSTRAINT [FK_PurchaseOrderSupplierPlanDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION
                    );
                    CREATE INDEX [IX_PurchaseOrderSupplierPlanDetails_SupplierPlanId_ProductId] ON [dbo].[PurchaseOrderSupplierPlanDetails]([SupplierPlanId], [ProductId]);
                END

                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'SupplierPlanDetailId') IS NULL
                    ALTER TABLE [dbo].[PurchaseOrderDetails] ADD [SupplierPlanDetailId] int NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseOrderDetails_SupplierPlanDetailId' AND object_id = OBJECT_ID(N'[dbo].[PurchaseOrderDetails]'))
                    CREATE INDEX [IX_PurchaseOrderDetails_SupplierPlanDetailId] ON [dbo].[PurchaseOrderDetails]([SupplierPlanDetailId]);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrderDetails_PurchaseOrderSupplierPlanDetails_SupplierPlanDetailId')
                   AND OBJECT_ID(N'[dbo].[PurchaseOrderSupplierPlanDetails]', N'U') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[PurchaseOrderDetails] WITH CHECK
                    ADD CONSTRAINT [FK_PurchaseOrderDetails_PurchaseOrderSupplierPlanDetails_SupplierPlanDetailId]
                    FOREIGN KEY ([SupplierPlanDetailId]) REFERENCES [dbo].[PurchaseOrderSupplierPlanDetails]([Id]) ON DELETE NO ACTION;
                END

                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'SupplierPlanDetailId') IS NULL
                    ALTER TABLE [dbo].[GoodsReceiptDetails] ADD [SupplierPlanDetailId] int NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GoodsReceiptDetails_SupplierPlanDetailId' AND object_id = OBJECT_ID(N'[dbo].[GoodsReceiptDetails]'))
                    CREATE INDEX [IX_GoodsReceiptDetails_SupplierPlanDetailId] ON [dbo].[GoodsReceiptDetails]([SupplierPlanDetailId]);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoodsReceiptDetails_PurchaseOrderSupplierPlanDetails_SupplierPlanDetailId')
                   AND OBJECT_ID(N'[dbo].[PurchaseOrderSupplierPlanDetails]', N'U') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[GoodsReceiptDetails] WITH CHECK
                    ADD CONSTRAINT [FK_GoodsReceiptDetails_PurchaseOrderSupplierPlanDetails_SupplierPlanDetailId]
                    FOREIGN KEY ([SupplierPlanDetailId]) REFERENCES [dbo].[PurchaseOrderSupplierPlanDetails]([Id]) ON DELETE NO ACTION;
                END

                IF COL_LENGTH(N'[dbo].[Lots]', N'CostUnitPrice') IS NULL
                    ALTER TABLE [dbo].[Lots] ADD [CostUnitPrice] decimal(18,2) NOT NULL CONSTRAINT [DF_Lots_CostUnitPrice] DEFAULT(0);
                IF COL_LENGTH(N'[dbo].[Lots]', N'CostPriceDate') IS NULL
                    ALTER TABLE [dbo].[Lots] ADD [CostPriceDate] datetime2 NULL;
                IF COL_LENGTH(N'[dbo].[Lots]', N'CostSourceType') IS NULL
                    ALTER TABLE [dbo].[Lots] ADD [CostSourceType] nvarchar(100) NULL;
                IF COL_LENGTH(N'[dbo].[Lots]', N'CostSourceRefId') IS NULL
                    ALTER TABLE [dbo].[Lots] ADD [CostSourceRefId] int NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Lots_CostSourceType_CostSourceRefId' AND object_id = OBJECT_ID(N'[dbo].[Lots]'))
                    CREATE INDEX [IX_Lots_CostSourceType_CostSourceRefId] ON [dbo].[Lots]([CostSourceType], [CostSourceRefId]);

                IF COL_LENGTH(N'[dbo].[OrderAllocations]', N'CostUnitPriceSnapshot') IS NULL
                    ALTER TABLE [dbo].[OrderAllocations] ADD [CostUnitPriceSnapshot] decimal(18,2) NULL;
                IF COL_LENGTH(N'[dbo].[OrderAllocations]', N'CostAmountSnapshot') IS NULL
                    ALTER TABLE [dbo].[OrderAllocations] ADD [CostAmountSnapshot] decimal(18,2) NULL;
                IF COL_LENGTH(N'[dbo].[OrderAllocations]', N'CostLotIdSnapshot') IS NULL
                    ALTER TABLE [dbo].[OrderAllocations] ADD [CostLotIdSnapshot] int NULL;

                IF COL_LENGTH(N'[dbo].[Lots]', N'CostUnitPrice') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[Lots]', N'CostSourceType') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[Lots]', N'CostSourceRefId') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[Lots]', N'GoodsReceiptDetailId') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'UnitPrice') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'PurchaseOrderDetailId') IS NOT NULL
                BEGIN
                    EXEC sp_executesql N'
                        UPDATE l
                        SET l.CostUnitPrice = ISNULL(gd.UnitPrice, 0),
                            l.CostSourceType = CASE WHEN l.CostSourceType IS NULL THEN N''LegacyPurchaseOrderDetail'' ELSE l.CostSourceType END,
                            l.CostSourceRefId = CASE WHEN l.CostSourceRefId IS NULL THEN gd.PurchaseOrderDetailId ELSE l.CostSourceRefId END
                        FROM [dbo].[Lots] l
                        INNER JOIN [dbo].[GoodsReceiptDetails] gd ON gd.[Id] = l.[GoodsReceiptDetailId]
                        WHERE l.CostUnitPrice = 0;';
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[OrderAllocations]', N'CostLotIdSnapshot') IS NOT NULL
                    ALTER TABLE [dbo].[OrderAllocations] DROP COLUMN [CostLotIdSnapshot];
                IF COL_LENGTH(N'[dbo].[OrderAllocations]', N'CostAmountSnapshot') IS NOT NULL
                    ALTER TABLE [dbo].[OrderAllocations] DROP COLUMN [CostAmountSnapshot];
                IF COL_LENGTH(N'[dbo].[OrderAllocations]', N'CostUnitPriceSnapshot') IS NOT NULL
                    ALTER TABLE [dbo].[OrderAllocations] DROP COLUMN [CostUnitPriceSnapshot];
                """);
        }
    }
}

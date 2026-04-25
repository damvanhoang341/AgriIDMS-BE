using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DomainLogicGoodsReceiptAndPO : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[FK_GoodsReceiptDetails_AspNetUsers_InspectedBy]', N'F') IS NOT NULL
                    ALTER TABLE [dbo].[GoodsReceiptDetails] DROP CONSTRAINT [FK_GoodsReceiptDetails_AspNetUsers_InspectedBy];

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GoodsReceiptDetails_InspectedBy' AND object_id = OBJECT_ID(N'[dbo].[GoodsReceiptDetails]'))
                    DROP INDEX [IX_GoodsReceiptDetails_InspectedBy] ON [dbo].[GoodsReceiptDetails];

                IF COL_LENGTH(N'[dbo].[GoodsReceipts]', N'TotalLossWeight') IS NOT NULL
                    ALTER TABLE [dbo].[GoodsReceipts] DROP COLUMN [TotalLossWeight];

                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ExpectedWeight') IS NULL
                    ALTER TABLE [dbo].[GoodsReceiptDetails] ADD [ExpectedWeight] decimal(18,3) NULL;

                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'OrderedWeight') IS NOT NULL
                    UPDATE [dbo].[GoodsReceiptDetails]
                    SET [ExpectedWeight] = [OrderedWeight]
                    WHERE [ExpectedWeight] IS NULL;

                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ExpectedWeight') IS NOT NULL
                BEGIN
                    UPDATE [dbo].[GoodsReceiptDetails] SET [ExpectedWeight] = 0 WHERE [ExpectedWeight] IS NULL;
                    ALTER TABLE [dbo].[GoodsReceiptDetails] ALTER COLUMN [ExpectedWeight] decimal(18,3) NOT NULL;
                END

                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'OrderedWeight') IS NOT NULL
                    ALTER TABLE [dbo].[GoodsReceiptDetails] DROP COLUMN [OrderedWeight];

                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'RejectWeight') IS NOT NULL
                    ALTER TABLE [dbo].[GoodsReceiptDetails] DROP COLUMN [RejectWeight];

                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ReceivedWeight') IS NULL
                    ALTER TABLE [dbo].[PurchaseOrderDetails] ADD [ReceivedWeight] decimal(18,3) NOT NULL CONSTRAINT [DF_PurchaseOrderDetails_ReceivedWeight] DEFAULT 0;

                IF COL_LENGTH(N'[dbo].[GoodsReceipts]', N'TolerancePercent') IS NOT NULL
                BEGIN
                    UPDATE [dbo].[GoodsReceipts] SET [TolerancePercent] = 2 WHERE [TolerancePercent] IS NULL;
                    ALTER TABLE [dbo].[GoodsReceipts] ALTER COLUMN [TolerancePercent] decimal(5,2) NOT NULL;
                END

                IF COL_LENGTH(N'[dbo].[GoodsReceipts]', N'PurchaseOrderId') IS NULL
                    ALTER TABLE [dbo].[GoodsReceipts] ADD [PurchaseOrderId] int NULL;

                IF COL_LENGTH(N'[dbo].[GoodsReceipts]', N'ReceiptCode') IS NULL
                    ALTER TABLE [dbo].[GoodsReceipts] ADD [ReceiptCode] nvarchar(50) NULL;

                IF COL_LENGTH(N'[dbo].[GoodsReceipts]', N'ReceiptCode') IS NOT NULL
                BEGIN
                    UPDATE [dbo].[GoodsReceipts]
                    SET [ReceiptCode] = CONCAT('GR-', YEAR([CreatedAt]), '-', FORMAT([Id], '00000'))
                    WHERE [ReceiptCode] IS NULL;

                    UPDATE [dbo].[GoodsReceipts]
                    SET [ReceiptCode] = CONCAT('GR-FIX-', [Id])
                    WHERE [ReceiptCode] IS NULL;

                    ALTER TABLE [dbo].[GoodsReceipts] ALTER COLUMN [ReceiptCode] nvarchar(50) NOT NULL;
                END

                IF COL_LENGTH(N'[dbo].[GoodsReceipts]', N'ReceivedBy') IS NULL
                    ALTER TABLE [dbo].[GoodsReceipts] ADD [ReceivedBy] nvarchar(450) NULL;

                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'UsableWeight') IS NOT NULL
                BEGIN
                    UPDATE [dbo].[GoodsReceiptDetails] SET [UsableWeight] = 0 WHERE [UsableWeight] IS NULL;
                    ALTER TABLE [dbo].[GoodsReceiptDetails] ALTER COLUMN [UsableWeight] decimal(18,3) NOT NULL;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GoodsReceipts_PurchaseOrderId' AND object_id = OBJECT_ID(N'[dbo].[GoodsReceipts]'))
                    CREATE INDEX [IX_GoodsReceipts_PurchaseOrderId] ON [dbo].[GoodsReceipts]([PurchaseOrderId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GoodsReceipts_ReceiptCode' AND object_id = OBJECT_ID(N'[dbo].[GoodsReceipts]'))
                    CREATE UNIQUE INDEX [IX_GoodsReceipts_ReceiptCode] ON [dbo].[GoodsReceipts]([ReceiptCode]);

                IF OBJECT_ID(N'[dbo].[FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId]', N'F') IS NULL
                    ALTER TABLE [dbo].[GoodsReceipts]
                    ADD CONSTRAINT [FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId]
                    FOREIGN KEY ([PurchaseOrderId]) REFERENCES [dbo].[PurchaseOrders]([Id]) ON DELETE NO ACTION;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipts_PurchaseOrders_PurchaseOrderId",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_PurchaseOrderId",
                table: "GoodsReceipts");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipts_ReceiptCode",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ReceivedWeight",
                table: "PurchaseOrderDetails");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderId",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ReceiptCode",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                table: "GoodsReceipts");

            migrationBuilder.AddColumn<decimal>(
                name: "OrderedWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.Sql("UPDATE GoodsReceiptDetails SET OrderedWeight = ExpectedWeight");
            migrationBuilder.AddColumn<decimal>(
                name: "RejectWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.DropColumn(
                name: "ExpectedWeight",
                table: "GoodsReceiptDetails");

            migrationBuilder.AlterColumn<decimal>(
                name: "TolerancePercent",
                table: "GoodsReceipts",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2,
                oldDefaultValue: 2m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalLossWeight",
                table: "GoodsReceipts",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "UsableWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)",
                oldPrecision: 18,
                oldScale: 3);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptDetails_InspectedBy",
                table: "GoodsReceiptDetails",
                column: "InspectedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptDetails_AspNetUsers_InspectedBy",
                table: "GoodsReceiptDetails",
                column: "InspectedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

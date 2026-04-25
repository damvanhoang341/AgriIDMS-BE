using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260425095000_HardenPurchaseRequestSchema")]
    public partial class HardenPurchaseRequestSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[PurchaseRequests]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PurchaseRequests] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [RequestCode] nvarchar(50) NOT NULL,
                        [Status] int NOT NULL DEFAULT(0),
                        [RequestedDate] datetime2 NOT NULL DEFAULT(SYSUTCDATETIME()),
                        [CreatedBy] nvarchar(450) NULL,
                        [Notes] nvarchar(500) NULL,
                        CONSTRAINT [PK_PurchaseRequests] PRIMARY KEY ([Id])
                    );
                END

                IF COL_LENGTH(N'[dbo].[PurchaseRequests]', N'RequestCode') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequests] ADD [RequestCode] nvarchar(50) NOT NULL DEFAULT(N'PR-MISSING');

                IF COL_LENGTH(N'[dbo].[PurchaseRequests]', N'Status') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequests] ADD [Status] int NOT NULL CONSTRAINT [DF_PurchaseRequests_Status] DEFAULT(0);

                IF COL_LENGTH(N'[dbo].[PurchaseRequests]', N'RequestedDate') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequests] ADD [RequestedDate] datetime2 NOT NULL CONSTRAINT [DF_PurchaseRequests_RequestedDate] DEFAULT(SYSUTCDATETIME());

                IF COL_LENGTH(N'[dbo].[PurchaseRequests]', N'CreatedBy') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequests] ADD [CreatedBy] nvarchar(450) NULL;

                IF COL_LENGTH(N'[dbo].[PurchaseRequests]', N'Notes') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequests] ADD [Notes] nvarchar(500) NULL;

                IF OBJECT_ID(N'[dbo].[PurchaseRequestDetails]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[PurchaseRequestDetails] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [PurchaseRequestId] int NOT NULL,
                        [ProductId] int NOT NULL,
                        [RequestedWeight] decimal(18,3) NOT NULL DEFAULT(0),
                        [AllocatedWeight] decimal(18,3) NOT NULL DEFAULT(0),
                        [TargetUnitPrice] decimal(18,2) NOT NULL DEFAULT(0),
                        CONSTRAINT [PK_PurchaseRequestDetails] PRIMARY KEY ([Id])
                    );
                END

                IF COL_LENGTH(N'[dbo].[PurchaseRequestDetails]', N'PurchaseRequestId') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequestDetails] ADD [PurchaseRequestId] int NOT NULL DEFAULT(0);

                IF COL_LENGTH(N'[dbo].[PurchaseRequestDetails]', N'ProductId') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequestDetails] ADD [ProductId] int NOT NULL DEFAULT(0);

                IF COL_LENGTH(N'[dbo].[PurchaseRequestDetails]', N'RequestedWeight') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequestDetails] ADD [RequestedWeight] decimal(18,3) NOT NULL DEFAULT(0);

                IF COL_LENGTH(N'[dbo].[PurchaseRequestDetails]', N'AllocatedWeight') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequestDetails] ADD [AllocatedWeight] decimal(18,3) NOT NULL DEFAULT(0);

                IF COL_LENGTH(N'[dbo].[PurchaseRequestDetails]', N'TargetUnitPrice') IS NULL
                    ALTER TABLE [dbo].[PurchaseRequestDetails] ADD [TargetUnitPrice] decimal(18,2) NOT NULL DEFAULT(0);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseRequests_CreatedBy' AND object_id = OBJECT_ID(N'[dbo].[PurchaseRequests]'))
                    CREATE INDEX [IX_PurchaseRequests_CreatedBy] ON [dbo].[PurchaseRequests]([CreatedBy]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseRequestDetails_PurchaseRequestId' AND object_id = OBJECT_ID(N'[dbo].[PurchaseRequestDetails]'))
                    CREATE INDEX [IX_PurchaseRequestDetails_PurchaseRequestId] ON [dbo].[PurchaseRequestDetails]([PurchaseRequestId]);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseRequestDetails_ProductId' AND object_id = OBJECT_ID(N'[dbo].[PurchaseRequestDetails]'))
                    CREATE INDEX [IX_PurchaseRequestDetails_ProductId] ON [dbo].[PurchaseRequestDetails]([ProductId]);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseRequestDetails_PurchaseRequests_PurchaseRequestId')
                   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseRequestDetails]') AND name = N'PurchaseRequestId')
                   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseRequests]') AND name = N'Id')
                BEGIN
                    DELETE d
                    FROM [dbo].[PurchaseRequestDetails] d
                    LEFT JOIN [dbo].[PurchaseRequests] r ON r.[Id] = d.[PurchaseRequestId]
                    WHERE r.[Id] IS NULL;

                    ALTER TABLE [dbo].[PurchaseRequestDetails]  WITH CHECK
                    ADD CONSTRAINT [FK_PurchaseRequestDetails_PurchaseRequests_PurchaseRequestId]
                    FOREIGN KEY([PurchaseRequestId]) REFERENCES [dbo].[PurchaseRequests]([Id]) ON DELETE CASCADE;
                END

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseRequestDetails_Products_ProductId')
                   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PurchaseRequestDetails]') AND name = N'ProductId')
                   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = N'Id')
                BEGIN
                    DELETE d
                    FROM [dbo].[PurchaseRequestDetails] d
                    LEFT JOIN [dbo].[Products] p ON p.[Id] = d.[ProductId]
                    WHERE p.[Id] IS NULL;

                    ALTER TABLE [dbo].[PurchaseRequestDetails] WITH CHECK
                    ADD CONSTRAINT [FK_PurchaseRequestDetails_Products_ProductId]
                    FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE NO ACTION;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentional no-op: hardening migration for production drift.
        }
    }
}

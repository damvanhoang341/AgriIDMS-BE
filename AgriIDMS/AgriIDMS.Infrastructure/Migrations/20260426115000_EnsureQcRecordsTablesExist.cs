using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260426115000_EnsureQcRecordsTablesExist")]
    public partial class EnsureQcRecordsTablesExist : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcRecords]', N'U') IS NULL
                   AND OBJECT_ID(N'[dbo].[Qcs]', N'U') IS NOT NULL
                BEGIN
                    EXEC sp_rename N'[dbo].[Qcs]', N'QcRecords';
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcRecords]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[QcRecords](
                        [Id] int NOT NULL IDENTITY(1,1),
                        [GoodsReceiptDetailId] int NOT NULL,
                        [InspectedWeight] decimal(18,3) NOT NULL CONSTRAINT [DF_QcRecords_InspectedWeight] DEFAULT(0),
                        [DamagedWeight] decimal(18,3) NOT NULL CONSTRAINT [DF_QcRecords_DamagedWeight] DEFAULT(0),
                        [PassedWeight] decimal(18,3) NOT NULL CONSTRAINT [DF_QcRecords_PassedWeight] DEFAULT(0),
                        [QCResult] int NOT NULL CONSTRAINT [DF_QcRecords_QCResult] DEFAULT(0),
                        [QCNote] nvarchar(500) NULL,
                        [InspectedBy] nvarchar(450) NULL,
                        [InspectedAt] datetime2 NULL,
                        CONSTRAINT [PK_QcRecords] PRIMARY KEY ([Id])
                    );
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcRecords]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[QcRecords]', N'UsableWeight') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[QcRecords]', N'InspectedWeight') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[QcRecords] ADD [InspectedWeight] decimal(18,3) NOT NULL
                        CONSTRAINT [DF_QcRecords_InspectedWeight_FromUsable] DEFAULT(0);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcRecords]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[QcRecords]', N'DamagedWeight') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[QcRecords] ADD [DamagedWeight] decimal(18,3) NOT NULL
                        CONSTRAINT [DF_QcRecords_DamagedWeight_Auto] DEFAULT(0);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcRecords]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[QcRecords]', N'PassedWeight') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[QcRecords] ADD [PassedWeight] decimal(18,3) NOT NULL
                        CONSTRAINT [DF_QcRecords_PassedWeight_Auto] DEFAULT(0);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcRecords]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[QcRecords]', N'UsableWeight') IS NOT NULL
                BEGIN
                    EXEC(N'
                        UPDATE q
                        SET q.[InspectedWeight] = CASE WHEN q.[InspectedWeight] = 0 THEN q.[UsableWeight] ELSE q.[InspectedWeight] END,
                            q.[PassedWeight] = CASE WHEN q.[PassedWeight] = 0 THEN q.[UsableWeight] ELSE q.[PassedWeight] END
                        FROM [dbo].[QcRecords] q
                    ');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcRecords]', N'U') IS NOT NULL
                   AND COL_LENGTH(N'[dbo].[QcRecords]', N'UsableWeight') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[QcRecords] DROP COLUMN [UsableWeight];
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcRecords]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_QcRecords_GoodsReceiptDetailId'
                          AND [object_id] = OBJECT_ID(N'[dbo].[QcRecords]')
                   )
                BEGIN
                    CREATE UNIQUE INDEX [IX_QcRecords_GoodsReceiptDetailId]
                    ON [dbo].[QcRecords]([GoodsReceiptDetailId]);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcRecords]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1
                        FROM sys.foreign_keys
                        WHERE [name] = N'FK_QcRecords_GoodsReceiptDetails_GoodsReceiptDetailId'
                   )
                BEGIN
                    ALTER TABLE [dbo].[QcRecords] WITH NOCHECK
                    ADD CONSTRAINT [FK_QcRecords_GoodsReceiptDetails_GoodsReceiptDetailId]
                    FOREIGN KEY([GoodsReceiptDetailId]) REFERENCES [dbo].[GoodsReceiptDetails]([Id]) ON DELETE NO ACTION;
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcClassificationDetails]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[QcClassificationDetails](
                        [Id] int NOT NULL IDENTITY(1,1),
                        [QcRecordId] int NOT NULL,
                        [ProductVariantId] int NOT NULL,
                        [Quantity] decimal(18,3) NOT NULL,
                        CONSTRAINT [PK_QcClassificationDetails] PRIMARY KEY ([Id])
                    );
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcClassificationDetails]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_QcClassificationDetails_QcRecordId'
                          AND [object_id] = OBJECT_ID(N'[dbo].[QcClassificationDetails]')
                   )
                BEGIN
                    CREATE INDEX [IX_QcClassificationDetails_QcRecordId]
                    ON [dbo].[QcClassificationDetails]([QcRecordId]);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcClassificationDetails]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1
                        FROM sys.indexes
                        WHERE [name] = N'IX_QcClassificationDetails_ProductVariantId'
                          AND [object_id] = OBJECT_ID(N'[dbo].[QcClassificationDetails]')
                   )
                BEGIN
                    CREATE INDEX [IX_QcClassificationDetails_ProductVariantId]
                    ON [dbo].[QcClassificationDetails]([ProductVariantId]);
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcClassificationDetails]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1
                        FROM sys.foreign_keys
                        WHERE [name] = N'FK_QcClassificationDetails_QcRecords_QcRecordId'
                   )
                BEGIN
                    ALTER TABLE [dbo].[QcClassificationDetails] WITH NOCHECK
                    ADD CONSTRAINT [FK_QcClassificationDetails_QcRecords_QcRecordId]
                    FOREIGN KEY([QcRecordId]) REFERENCES [dbo].[QcRecords]([Id]) ON DELETE CASCADE;
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[dbo].[QcClassificationDetails]', N'U') IS NOT NULL
                   AND NOT EXISTS (
                        SELECT 1
                        FROM sys.foreign_keys
                        WHERE [name] = N'FK_QcClassificationDetails_ProductVariants_ProductVariantId'
                   )
                BEGIN
                    ALTER TABLE [dbo].[QcClassificationDetails] WITH NOCHECK
                    ADD CONSTRAINT [FK_QcClassificationDetails_ProductVariants_ProductVariantId]
                    FOREIGN KEY([ProductVariantId]) REFERENCES [dbo].[ProductVariants]([Id]) ON DELETE NO ACTION;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

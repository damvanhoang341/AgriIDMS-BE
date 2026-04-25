using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260425190500_AllowNullProductVariantIdOnPurchaseOrderDetails")]
    public partial class AllowNullProductVariantIdOnPurchaseOrderDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[PurchaseOrderDetails]', N'ProductVariantId') IS NOT NULL
                   AND EXISTS (
                        SELECT 1
                        FROM sys.columns c
                        WHERE c.object_id = OBJECT_ID(N'[dbo].[PurchaseOrderDetails]')
                          AND c.name = N'ProductVariantId'
                          AND c.is_nullable = 0
                   )
                BEGIN
                    ALTER TABLE [dbo].[PurchaseOrderDetails] ALTER COLUMN [ProductVariantId] int NULL;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep down migration safe for drifted databases: do not force NOT NULL back
            // because existing rows may contain NULL values.
        }
    }
}


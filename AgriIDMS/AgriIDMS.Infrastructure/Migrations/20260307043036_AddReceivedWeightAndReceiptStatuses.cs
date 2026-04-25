using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceivedWeightAndReceiptStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'ReceivedWeight') IS NULL
                    ALTER TABLE [dbo].[GoodsReceiptDetails] ADD [ReceivedWeight] decimal(18,3) NOT NULL CONSTRAINT [DF_GoodsReceiptDetails_ReceivedWeight] DEFAULT(0);

                IF COL_LENGTH(N'[dbo].[GoodsReceiptDetails]', N'OrderedWeight') IS NOT NULL
                    UPDATE [dbo].[GoodsReceiptDetails] SET [ReceivedWeight] = [OrderedWeight] WHERE [ReceivedWeight] = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceivedWeight",
                table: "GoodsReceiptDetails");
        }
    }
}

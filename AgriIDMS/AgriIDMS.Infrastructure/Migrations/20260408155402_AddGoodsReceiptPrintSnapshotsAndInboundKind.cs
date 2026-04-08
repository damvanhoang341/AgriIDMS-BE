using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptPrintSnapshotsAndInboundKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InboundReceiptKind",
                table: "GoodsReceipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NonPoReason",
                table: "GoodsReceipts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrintSnapshotAfterApproveJson",
                table: "GoodsReceipts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrintSnapshotAfterQcJson",
                table: "GoodsReceipts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE GoodsReceipts SET InboundReceiptKind = 1 WHERE PurchaseOrderId IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InboundReceiptKind",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "NonPoReason",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "PrintSnapshotAfterApproveJson",
                table: "GoodsReceipts");

            migrationBuilder.DropColumn(
                name: "PrintSnapshotAfterQcJson",
                table: "GoodsReceipts");
        }
    }
}

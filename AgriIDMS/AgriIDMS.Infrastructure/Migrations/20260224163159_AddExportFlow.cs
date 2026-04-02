using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExportFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestId",
                table: "InventoryRequest",
                newName: "Id");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentCapacity",
                table: "Slots",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "FulfilledQuantity",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShortageQuantity",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ExportReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExportCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportReceipts_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExportReceipts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OrderDetailId = table.Column<int>(type: "int", nullable: false),
                    BoxId = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PickedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAllocations", x => x.Id);
                    table.CheckConstraint("CK_OrderAllocation_PickedQty_Valid", "[PickedQuantity] IS NULL OR [PickedQuantity] >= 0");
                    table.CheckConstraint("CK_OrderAllocation_ReservedQty_Positive", "[ReservedQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_OrderAllocations_Boxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "Boxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderAllocations_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderAllocations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    TransactionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExportDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExportReceiptId = table.Column<int>(type: "int", nullable: false),
                    BoxId = table.Column<int>(type: "int", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportDetails", x => x.Id);
                    table.CheckConstraint("CK_ExportDetail_ActualQty_Positive", "[ActualQuantity] > 0");
                    table.ForeignKey(
                        name: "FK_ExportDetails_Boxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "Boxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExportDetails_ExportReceipts_ExportReceiptId",
                        column: x => x.ExportReceiptId,
                        principalTable: "ExportReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RefundTransactionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lots_ProductId_ExpiryDate_Status",
                table: "Lots",
                columns: new[] { "ProductId", "ExpiryDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ReferenceType_ReferenceRequestId",
                table: "InventoryTransactions",
                columns: new[] { "ReferenceType", "ReferenceRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_Boxes_LotId_Status_CreatedAt",
                table: "Boxes",
                columns: new[] { "LotId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportDetails_BoxId",
                table: "ExportDetails",
                column: "BoxId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportDetails_ExportReceiptId",
                table: "ExportDetails",
                column: "ExportReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportDetails_ExportReceiptId_BoxId",
                table: "ExportDetails",
                columns: new[] { "ExportReceiptId", "BoxId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_CreatedAt",
                table: "ExportReceipts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_CreatedBy",
                table: "ExportReceipts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_ExportCode",
                table: "ExportReceipts",
                column: "ExportCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_OrderId",
                table: "ExportReceipts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportReceipts_Status",
                table: "ExportReceipts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAllocations_BoxId",
                table: "OrderAllocations",
                column: "BoxId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAllocations_OrderDetailId_BoxId",
                table: "OrderAllocations",
                columns: new[] { "OrderDetailId", "BoxId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderAllocations_OrderId",
                table: "OrderAllocations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAllocations_Status",
                table: "OrderAllocations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionCode",
                table: "Payments",
                column: "TransactionCode",
                unique: true,
                filter: "[TransactionCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PaymentId",
                table: "Refunds",
                column: "PaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportDetails");

            migrationBuilder.DropTable(
                name: "OrderAllocations");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "ExportReceipts");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Lots_ProductId_ExpiryDate_Status",
                table: "Lots");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ReferenceType_ReferenceRequestId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Boxes_LotId_Status_CreatedAt",
                table: "Boxes");

            migrationBuilder.DropColumn(
                name: "FulfilledQuantity",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ShortageQuantity",
                table: "OrderDetails");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "InventoryRequest",
                newName: "RequestId");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentCapacity",
                table: "Slots",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldDefaultValue: 0m);
        }
    }
}

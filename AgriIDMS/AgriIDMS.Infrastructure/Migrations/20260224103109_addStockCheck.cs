using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addStockCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReferenceId",
                table: "InventoryTransactions",
                newName: "ReferenceRequestId");

            migrationBuilder.AddColumn<int>(
                name: "InventoryRequestId",
                table: "InventoryTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryRequest",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestType = table.Column<int>(type: "int", nullable: false),
                    ReferenceType = table.Column<int>(type: "int", nullable: true),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryRequest", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_InventoryRequest_AspNetUsers_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryRequest_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    CheckType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SnapshotAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLockedSnapshot = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockChecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockChecks_AspNetUsers_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockChecks_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockChecks_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockCheckDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockCheckId = table.Column<int>(type: "int", nullable: false),
                    BoxId = table.Column<int>(type: "int", nullable: false),
                    SnapshotWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentSystemWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CountedWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DifferenceWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    VarianceType = table.Column<int>(type: "int", nullable: true),
                    CountedBy = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CountedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockCheckDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockCheckDetails_AspNetUsers_CountedBy",
                        column: x => x.CountedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCheckDetails_Boxes_BoxId",
                        column: x => x.BoxId,
                        principalTable: "Boxes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockCheckDetails_StockChecks_StockCheckId",
                        column: x => x.StockCheckId,
                        principalTable: "StockChecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ReferenceRequestId",
                table: "InventoryTransactions",
                column: "ReferenceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequest_ApprovedBy",
                table: "InventoryRequest",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryRequest_CreatedBy",
                table: "InventoryRequest",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockCheckDetails_BoxId",
                table: "StockCheckDetails",
                column: "BoxId");

            migrationBuilder.CreateIndex(
                name: "IX_StockCheckDetails_CountedBy",
                table: "StockCheckDetails",
                column: "CountedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockCheckDetails_StockCheckId",
                table: "StockCheckDetails",
                column: "StockCheckId");

            migrationBuilder.CreateIndex(
                name: "IX_StockChecks_ApprovedBy",
                table: "StockChecks",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockChecks_CreatedBy",
                table: "StockChecks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockChecks_Status",
                table: "StockChecks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockChecks_WarehouseId",
                table: "StockChecks",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_InventoryRequest_ReferenceRequestId",
                table: "InventoryTransactions",
                column: "ReferenceRequestId",
                principalTable: "InventoryRequest",
                principalColumn: "RequestId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_InventoryRequest_ReferenceRequestId",
                table: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "InventoryRequest");

            migrationBuilder.DropTable(
                name: "StockCheckDetails");

            migrationBuilder.DropTable(
                name: "StockChecks");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ReferenceRequestId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "InventoryRequestId",
                table: "InventoryTransactions");

            migrationBuilder.RenameColumn(
                name: "ReferenceRequestId",
                table: "InventoryTransactions",
                newName: "ReferenceId");
        }
    }
}

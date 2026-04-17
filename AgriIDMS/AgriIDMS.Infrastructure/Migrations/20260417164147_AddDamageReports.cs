using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDamageReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DamageReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    TargetCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LotId = table.Column<int>(type: "int", nullable: true),
                    LotCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WarehouseId = table.Column<int>(type: "int", nullable: true),
                    WarehouseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DamageReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DamagePercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    SuggestedDiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EvidenceImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReportedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReportedByUsername = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedByUsername = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AppliedDiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DamageReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DamageReports_AspNetUsers_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DamageReports_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DamageReports_Lots_LotId",
                        column: x => x.LotId,
                        principalTable: "Lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DamageReports_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DamageReports_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_LotId",
                table: "DamageReports",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_ProductVariantId",
                table: "DamageReports",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_ReportedAt",
                table: "DamageReports",
                column: "ReportedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_ReportedByUserId",
                table: "DamageReports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_ReviewedByUserId",
                table: "DamageReports",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_Status",
                table: "DamageReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DamageReports_WarehouseId",
                table: "DamageReports",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DamageReports");
        }
    }
}

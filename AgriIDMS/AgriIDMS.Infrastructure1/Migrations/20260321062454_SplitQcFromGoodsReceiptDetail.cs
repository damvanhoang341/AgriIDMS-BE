using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitQcFromGoodsReceiptDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Qcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoodsReceiptDetailId = table.Column<int>(type: "int", nullable: false),
                    UsableWeight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QCResult = table.Column<int>(type: "int", nullable: false),
                    QCNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InspectedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    InspectedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Qcs_GoodsReceiptDetails_GoodsReceiptDetailId",
                        column: x => x.GoodsReceiptDetailId,
                        principalTable: "GoodsReceiptDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Qcs_GoodsReceiptDetailId",
                table: "Qcs",
                column: "GoodsReceiptDetailId",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO Qcs (GoodsReceiptDetailId, UsableWeight, QCResult, QCNote, InspectedBy, InspectedAt)
                SELECT Id, COALESCE(UsableWeight, 0), QCResult, QCNote, InspectedBy, InspectedAt
                FROM GoodsReceiptDetails
                WHERE UsableWeight IS NOT NULL
                   OR QCResult <> 0
                   OR QCNote IS NOT NULL
                   OR InspectedBy IS NOT NULL
                   OR InspectedAt IS NOT NULL
                """);

            migrationBuilder.DropColumn(
                name: "InspectedAt",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "InspectedBy",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "QCNote",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "QCResult",
                table: "GoodsReceiptDetails");

            migrationBuilder.DropColumn(
                name: "UsableWeight",
                table: "GoodsReceiptDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InspectedAt",
                table: "GoodsReceiptDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectedBy",
                table: "GoodsReceiptDetails",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QCNote",
                table: "GoodsReceiptDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QCResult",
                table: "GoodsReceiptDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UsableWeight",
                table: "GoodsReceiptDetails",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE grd
                SET UsableWeight = q.UsableWeight,
                    QCResult = q.QCResult,
                    QCNote = q.QCNote,
                    InspectedBy = q.InspectedBy,
                    InspectedAt = q.InspectedAt
                FROM GoodsReceiptDetails grd
                INNER JOIN Qcs q ON q.GoodsReceiptDetailId = grd.Id
                """);

            migrationBuilder.DropTable(
                name: "Qcs");
        }
    }
}

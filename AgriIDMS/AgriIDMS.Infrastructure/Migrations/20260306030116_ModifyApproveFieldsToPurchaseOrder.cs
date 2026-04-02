using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifyApproveFieldsToPurchaseOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ApprovedUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_CreatedUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ApprovedUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CreatedUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ApprovedUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CreatedUserId",
                table: "PurchaseOrders");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "PurchaseOrders",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "PurchaseOrders",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ApprovedBy",
                table: "PurchaseOrders",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedBy",
                table: "PurchaseOrders",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ApprovedBy",
                table: "PurchaseOrders",
                column: "ApprovedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_CreatedBy",
                table: "PurchaseOrders",
                column: "CreatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ApprovedBy",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_CreatedBy",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_ApprovedBy",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CreatedBy",
                table: "PurchaseOrders");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedBy",
                table: "PurchaseOrders",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedUserId",
                table: "PurchaseOrders",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedUserId",
                table: "PurchaseOrders",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_ApprovedUserId",
                table: "PurchaseOrders",
                column: "ApprovedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CreatedUserId",
                table: "PurchaseOrders",
                column: "CreatedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_ApprovedUserId",
                table: "PurchaseOrders",
                column: "ApprovedUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_CreatedUserId",
                table: "PurchaseOrders",
                column: "CreatedUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}

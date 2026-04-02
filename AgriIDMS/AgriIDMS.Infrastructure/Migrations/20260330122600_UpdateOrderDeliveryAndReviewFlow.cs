using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderDeliveryAndReviewFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerId",
                table: "Reviews",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Freshness",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Packaging",
                table: "Reviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews",
                column: "CustomerId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Review_Freshness",
                table: "Reviews",
                sql: "[Freshness] >= 1 AND [Freshness] <= 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Review_Packaging",
                table: "Reviews",
                sql: "[Packaging] >= 1 AND [Packaging] <= 5");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveredAt",
                table: "Orders",
                column: "DeliveredAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_AspNetUsers_CustomerId",
                table: "Reviews",
                column: "CustomerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_AspNetUsers_CustomerId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_CustomerId",
                table: "Reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Review_Freshness",
                table: "Reviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Review_Packaging",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Freshness",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Packaging",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "Orders");
        }
    }
}

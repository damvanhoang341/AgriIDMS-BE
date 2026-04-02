using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBackorderExpiryNotifiedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BackorderExpiryNotifiedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BackorderExpiryNotifiedAt",
                table: "Orders",
                column: "BackorderExpiryNotifiedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_BackorderExpiryNotifiedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BackorderExpiryNotifiedAt",
                table: "Orders");
        }
    }
}

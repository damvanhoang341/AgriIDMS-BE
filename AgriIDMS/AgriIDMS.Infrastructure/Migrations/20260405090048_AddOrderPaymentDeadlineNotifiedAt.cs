using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentDeadlineNotifiedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentDeadlineNotifiedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PaymentDeadlineNotifiedAt",
                table: "Orders",
                column: "PaymentDeadlineNotifiedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PaymentDeadlineNotifiedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentDeadlineNotifiedAt",
                table: "Orders");
        }
    }
}

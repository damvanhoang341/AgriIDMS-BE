using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfillmentTypeForOrderFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FulfillmentType",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Delivery");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FulfillmentType",
                table: "Orders",
                column: "FulfillmentType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_FulfillmentType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FulfillmentType",
                table: "Orders");
        }
    }
}

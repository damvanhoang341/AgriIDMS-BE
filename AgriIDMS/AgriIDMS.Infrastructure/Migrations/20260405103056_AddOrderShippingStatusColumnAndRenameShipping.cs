using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippingStatusColumnAndRenameShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShippingStatus",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "None");

            // Legacy OrderStatus "Shipping" → ApprovedExport; backfill ShippingStatus for existing rows.
            migrationBuilder.Sql(
                """
                UPDATE [Orders] SET [Status] = N'ApprovedExport' WHERE [Status] = N'Shipping';
                UPDATE [Orders] SET [ShippingStatus] = N'DeliveredShip' WHERE [Status] IN (N'Delivered', N'Completed');
                UPDATE [Orders] SET [ShippingStatus] = N'ShippingFailed' WHERE [Status] = N'FailedDelivery';
                UPDATE [Orders] SET [ShippingStatus] = N'None' WHERE [Status] = N'Returned';
                UPDATE [Orders] SET [ShippingStatus] = N'ShippingPendingPickup' WHERE [FulfillmentType] = N'Delivery' AND [Status] = N'ApprovedExport';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingStatus",
                table: "Orders");
        }
    }
}

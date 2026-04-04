using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgriIDMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentTiming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentTiming",
                table: "Orders",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PayAfter");

            // Đơn online: trả sau (thu khi giao / tiền mặt Pending). POS giao hàng: trả trước. POS TakeAway PayBeforePick: PayBefore.
            migrationBuilder.Sql(@"
UPDATE Orders SET PaymentTiming = N'PayBefore'
WHERE Source = N'POS' AND FulfillmentType <> N'TakeAway';

UPDATE Orders SET PaymentTiming = N'PayBefore'
WHERE Source = N'POS' AND FulfillmentType = N'TakeAway' AND PosCheckoutTiming = N'PayBeforePick';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentTiming",
                table: "Orders");
        }
    }
}
